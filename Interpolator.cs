using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

public class Interpolator : MonoBehaviour
{
    public enum InterpolationType { LINEAR, EASEIN, EASEOUT, CUSTOM }
    public enum Attribute { POSITION, SCALE, ROTATION, FLOAT }

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

        public object cToChange;
        public FieldInfo cField;
        public float cInit;
        public float cTarget;
    }
    private static Dictionary<object, Target> targets = new Dictionary<object, Target>();

    public static int AddTarget(object toChange, Attribute attribute, object target, float time, InterpolationType interpolationType, Action<object> customInterpolator = null, Hashtable customParams = null, float easeStrength = 3, FieldInfo field = null)
    {
        Target nTarget = new Target();
        nTarget.attribute = attribute;
        nTarget.time = time;
        nTarget.interpolationType = interpolationType;
        nTarget.customInterpolator = customInterpolator;
        nTarget.customParams = customParams;
        nTarget.easeStrength = easeStrength;
        nTarget.initTime = Time.time;

        if (attribute == Attribute.FLOAT)
        {
            nTarget.cToChange = toChange;
            nTarget.cTarget = (float)target;
            nTarget.cInit = (float)field.GetValue(toChange);
            nTarget.cField = field;
        }
        else
        {
            nTarget.toChange = (Transform)toChange;
            nTarget.target = (Vector3)target;
            if (attribute == Attribute.POSITION) { nTarget.init = nTarget.toChange.position; }
            else if (attribute == Attribute.SCALE) { nTarget.init = nTarget.toChange.localScale; }
            else if (attribute == Attribute.ROTATION) { nTarget.init = nTarget.toChange.rotation.eulerAngles; }
        }

        int id = UnityEngine.Random.Range(int.MinValue, int.MaxValue);
        while (targets.ContainsKey(id))
        {
            id = UnityEngine.Random.Range(int.MinValue, int.MaxValue);
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
                    targets.Remove(i);
                }
            }
            else
            {
                Vector3 current = (targets[i].attribute == Attribute.POSITION) ? (targets[i].toChange.position) : ((targets[i].attribute == Attribute.ROTATION) ? (targets[i].toChange.rotation.eulerAngles) : (targets[i].toChange.localScale));
                Vector3 delta = Vector3.zero;

                if (targets[i].interpolationType == InterpolationType.LINEAR)
                {
                    delta = (targets[i].target - targets[i].init) * Time.deltaTime / targets[i].time;
                }
                else if (targets[i].interpolationType == InterpolationType.EASEOUT)
                {
                    float x = (Time.time - targets[i].initTime) / targets[i].time;
                    float m = 1 - ((1 - x) / Mathf.Pow(1f + (targets[i].easeStrength * x), 2f));
                    delta = (targets[i].target - targets[i].init) * m - current + targets[i].init;
                }
                else if (targets[i].interpolationType == InterpolationType.EASEIN)
                {
                    float x = 1 - ((Time.time - targets[i].initTime) / targets[i].time);
                    float m = 1 - (1 - ((1 - x) / Mathf.Pow(1f + (targets[i].easeStrength * x), 2f)));
                    delta = (targets[i].target - targets[i].init) * m - current + targets[i].init;
                }
                else if (targets[i].interpolationType == InterpolationType.CUSTOM)
                {
                    targets[i].customInterpolator(targets[i].customParams);
                    delta = (Vector3)targets[i].customParams["RETURN"] - current;
                }

                if (Vector3.Distance(current, targets[i].target) > delta.magnitude)
                {
                    if (targets[i].attribute == Attribute.POSITION)
                    {
                        targets[i].toChange.position += delta;
                    }
                    else if (targets[i].attribute == Attribute.ROTATION)
                    {
                        targets[i].toChange.rotation = Quaternion.Euler(targets[i].toChange.rotation.eulerAngles + delta);
                    }
                    else if (targets[i].attribute == Attribute.SCALE)
                    {
                        targets[i].toChange.localScale += delta;
                    }
                }
                else
                {
                    if (targets[i].attribute == Attribute.POSITION)
                    {
                        targets[i].toChange.position = targets[i].target;
                    }
                    else if (targets[i].attribute == Attribute.ROTATION)
                    {
                        targets[i].toChange.rotation = Quaternion.Euler(targets[i].target);
                    }
                    else if (targets[i].attribute == Attribute.SCALE)
                    {
                        targets[i].toChange.localScale = targets[i].target;
                    }
                    targets.Remove(i);
                }
            }
        }
    }
}
