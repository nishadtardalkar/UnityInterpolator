using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

public class Interpolator : MonoBehaviour
{
    public enum InterpolationType { LINEAR, EASEIN, EASEOUT, CUSTOM }
    public enum Attribute { POSITION, SCALE, ROTATION_EULER, FLOAT, ROTATION_ADDITIVE, NULL }

    private class Target
    {
        public Transform toChange;
        public Vector3 target;
        public float time;
        public InterpolationType interpolationType;
        public Attribute attribute;
        public Vector3 init;
        public float easeStrength;
        public Action<object> customInterpolator;
        public Hashtable customParams;
        public float initTime;
        public Vector3 current;
        public float percent;
        public bool active;
        public int nextActive;

        public object cToChange;
        public FieldInfo cField;
        public float cInit;
        public float cTarget;

        public Func<float> getValue;
        public Action<float> setValue;
    }
    private static Dictionary<object, Target> targets = new Dictionary<object, Target>();

    public static int AddTarget(object toChange, Attribute attribute, object target, float time, InterpolationType interpolationType,
        Action<object> customInterpolator = null,
        Hashtable customParams = null,
        float easeStrength = 3,
        FieldInfo field = null,
        bool cancelPrevious = true,
        Func<float> getValue = null,
        Action<float> setValue = null)
    {
        Target nTarget = new Target();
        nTarget.attribute = attribute;
        nTarget.time = time;
        nTarget.interpolationType = interpolationType;
        nTarget.customInterpolator = customInterpolator;
        nTarget.customParams = customParams;
        nTarget.easeStrength = easeStrength;
        nTarget.initTime = Time.time;
        nTarget.percent = 0f;
        nTarget.active = true;
        nTarget.nextActive = -1;

        if (attribute == Attribute.FLOAT)
        {
            nTarget.cToChange = toChange;
            nTarget.cTarget = (float)target;
            nTarget.cInit = (float)field.GetValue(toChange);
            nTarget.cField = field;
        }
        else if (attribute == Attribute.NULL)
        {
            nTarget.cTarget = (float)target;
            nTarget.cInit = getValue();
            nTarget.getValue = getValue;
            nTarget.setValue = setValue;
        }
        else
        {
            nTarget.toChange = (Transform)toChange;
            nTarget.target = (Vector3)target;
            if (attribute == Attribute.POSITION)
            {
                nTarget.init = nTarget.toChange.position;
            }
            else if (attribute == Attribute.SCALE)
            {
                nTarget.init = nTarget.toChange.localScale;
            }
            else if (attribute == Attribute.ROTATION_ADDITIVE || attribute == Attribute.ROTATION_EULER)
            {
                nTarget.init = nTarget.toChange.rotation.eulerAngles;
            }
            nTarget.current = nTarget.init;
        }

        int id = UnityEngine.Random.Range(0, int.MaxValue);
        while (targets.ContainsKey(id))
        {
            id = UnityEngine.Random.Range(0, int.MaxValue);
        }
        List<object> keys = new List<object>(targets.Keys);
        foreach (object t in keys)
        {
            if (attribute == Attribute.FLOAT)
            {
                if (targets[t].cToChange == toChange)
                {
                    if (cancelPrevious)
                    {
                        targets.Remove(t);
                    }
                    else
                    {
                        Target x = targets[t];
                        while (x.nextActive != -1) { x = targets[x.nextActive]; }
                        nTarget.active = false;
                        x.nextActive = id;
                        break;
                    }
                }
            }
            else if (targets[t].toChange == (Transform)toChange && targets[t].attribute == attribute)
            {
                if (cancelPrevious)
                {
                    targets.Remove(t);
                }
                else 
                {
                    object x = t;
                    while (targets[x].nextActive != -1) { x = targets[x].nextActive; }
                    nTarget.active = false;
                    targets[x].nextActive = id;
                    break;
                }
            }
        }
        targets.Add(id, nTarget);
        
        return id;
    }

    public static bool IsActive(object id)
    {
        if (targets.ContainsKey(id))
        {
            return true;
        }
        return false;
    }

    public static void RemoveTarget(object id)
    {
        if (targets.ContainsKey(id))
        {
            targets.Remove(id);
        }
    }

    void Update()
    {
        object[] keys = new object[targets.Keys.Count];
        targets.Keys.CopyTo(keys, 0);
        foreach (object i in keys) 
        {
            if (!targets[i].active) { continue; }
            if (targets[i].attribute == Attribute.FLOAT)
            {
                float current = (float)targets[i].cField.GetValue(targets[i].cToChange);
                float delta = 0;
                if (targets[i].interpolationType == InterpolationType.LINEAR)
                {
                    delta = (targets[i].cTarget - targets[i].cInit) * Time.deltaTime / targets[i].time;
                }
                else if (targets[i].interpolationType == InterpolationType.EASEOUT)
                {
                    float x = (Time.time - targets[i].initTime) / targets[i].time;
                    float m = 1 - ((1 - x) / Mathf.Pow(1f + (targets[i].easeStrength * x), 2f));
                    delta = (targets[i].cTarget - targets[i].cInit) * m - current + targets[i].cInit;
                }
                else if (targets[i].interpolationType == InterpolationType.EASEIN)
                {
                    float x = 1 - ((Time.time - targets[i].initTime) / targets[i].time);
                    float m = 1 - (1 - ((1 - x) / Mathf.Pow(1f + (targets[i].easeStrength * x), 2f)));
                    delta = (targets[i].cTarget - targets[i].cInit) * m - current + targets[i].cInit;
                }
                else if (targets[i].interpolationType == InterpolationType.CUSTOM)
                {
                    targets[i].customInterpolator(targets[i].customParams);
                    delta = (float)targets[i].customParams["RETURN"] - current;
                }
                if (Mathf.Abs(targets[i].cTarget - current) > Math.Abs(delta))
                {
                    targets[i].cField.SetValue(targets[i].cToChange, current + delta);
                }
                else
                {
                    targets[i].cField.SetValue(targets[i].cToChange, targets[i].cTarget);
                    if (targets[i].nextActive != -1)
                    {
                        targets[targets[i].nextActive].active = true;
                    }
                    targets.Remove(i);
                }
            }
            else if (targets[i].attribute == Attribute.NULL)
            {
                float current = targets[i].getValue();
                float delta = 0;
                if (targets[i].interpolationType == InterpolationType.LINEAR)
                {
                    delta = (targets[i].cTarget - targets[i].cInit) * Time.deltaTime / targets[i].time;
                }
                else if (targets[i].interpolationType == InterpolationType.EASEOUT)
                {
                    float x = (Time.time - targets[i].initTime) / targets[i].time;
                    float m = 1 - ((1 - x) / Mathf.Pow(1f + (targets[i].easeStrength * x), 2f));
                    delta = (targets[i].cTarget - targets[i].cInit) * m - current + targets[i].cInit;
                }
                else if (targets[i].interpolationType == InterpolationType.EASEIN)
                {
                    float x = 1 - ((Time.time - targets[i].initTime) / targets[i].time);
                    float m = 1 - (1 - ((1 - x) / Mathf.Pow(1f + (targets[i].easeStrength * x), 2f)));
                    delta = (targets[i].cTarget - targets[i].cInit) * m - current + targets[i].cInit;
                }
                else if (targets[i].interpolationType == InterpolationType.CUSTOM)
                {
                    targets[i].customInterpolator(targets[i].customParams);
                    delta = (float)targets[i].customParams["RETURN"] - current;
                }
                if (Mathf.Abs(targets[i].cTarget - current) > Math.Abs(delta))
                {
                    targets[i].setValue(current + delta);
                }
                else
                {
                    targets[i].setValue(targets[i].cTarget);
                    if (targets[i].nextActive != -1)
                    {
                        targets[targets[i].nextActive].active = true;
                    }
                    targets.Remove(i);
                }
            }
            else
            {
                //Vector3 current = (targets[i].attribute == Attribute.POSITION) ? (targets[i].toChange.position) : ((targets[i].attribute == Attribute.ROTATION) ? (targets[i].toChange.rotation.eulerAngles) : (targets[i].toChange.localScale));
                Vector3 delta = Vector3.zero;
                if (targets[i].interpolationType == InterpolationType.LINEAR)
                {
                    delta = (targets[i].target - targets[i].init) * Time.deltaTime / targets[i].time;
                    targets[i].percent += Time.deltaTime / targets[i].time;
                }
                else if (targets[i].interpolationType == InterpolationType.EASEOUT)
                {
                    float x = (Time.time - targets[i].initTime) / targets[i].time;
                    float m = 1 - ((1 - x) / Mathf.Pow(1f + (targets[i].easeStrength * x), 2f));
                    targets[i].percent = m;
                    delta = (targets[i].target - targets[i].init) * m - targets[i].current + targets[i].init;
                }
                else if (targets[i].interpolationType == InterpolationType.EASEIN)
                {
                    float x = 1 - ((Time.time - targets[i].initTime) / targets[i].time);
                    float m = 1 - (1 - ((1 - x) / Mathf.Pow(1f + (targets[i].easeStrength * x), 2f)));
                    targets[i].percent = m;
                    delta = (targets[i].target - targets[i].init) * m - targets[i].current + targets[i].init;
                }
                else if (targets[i].interpolationType == InterpolationType.CUSTOM)
                {
                    targets[i].customInterpolator(targets[i].customParams);
                    delta = (Vector3)targets[i].customParams["RETURN"] - targets[i].current;
                    targets[i].percent = (float)targets[i].customParams["PERCENT"];
                }

                //if (Time.time - targets[i].initTime < targets[i].time) 
                if (Vector3.Distance(targets[i].target, targets[i].current) > delta.magnitude)
                {
                    if (targets[i].attribute == Attribute.POSITION)
                    {
                        targets[i].toChange.position += delta;
                    }
                    else if (targets[i].attribute == Attribute.ROTATION_EULER)
                    {
                        targets[i].toChange.rotation = Quaternion.Lerp(Quaternion.Euler(targets[i].init), Quaternion.Euler(targets[i].target), targets[i].percent);
                    }
                    else if (targets[i].attribute == Attribute.ROTATION_ADDITIVE)
                    {
                        targets[i].toChange.rotation *= Quaternion.Euler(delta);
                    }
                    else if (targets[i].attribute == Attribute.SCALE)
                    {
                        targets[i].toChange.localScale += delta;
                    }
                    targets[i].current += delta;
                }
                else
                {
                    if (targets[i].attribute == Attribute.POSITION)
                    {
                        targets[i].toChange.position = targets[i].target;
                    }
                    else if (targets[i].attribute == Attribute.ROTATION_ADDITIVE || targets[i].attribute == Attribute.ROTATION_EULER)
                    {
                        targets[i].toChange.rotation = Quaternion.Euler(targets[i].target);
                    }
                    else if (targets[i].attribute == Attribute.SCALE)
                    {
                        targets[i].toChange.localScale = targets[i].target;
                    }
                    if (targets[i].nextActive != -1)
                    {
                        targets[targets[i].nextActive].active = true;
                    }
                    targets.Remove(i);
                }
            }
        }
    }
}
