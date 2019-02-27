using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Interpolator : MonoBehaviour
{
    public enum InterpolationType { LINEAR, EASEIN, EASEOUT, CUSTOM }
    public enum Attribute { POSITION, SCALE, ROTATION }

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
    }
    private static List<Target> targets = new List<Target>();
    private static HashSet<int> active = new HashSet<int>();

    void Start()
    {
        
    }

    public static int AddTarget(Transform toChange, Attribute attribute, Vector3 target, float time, InterpolationType interpolationType, Action<object> customInterpolator = null, Hashtable customParams = null, float easeStrength = 3)
    {
        Target nTarget = new Target();
        nTarget.attribute = attribute;
        nTarget.toChange = toChange;
        nTarget.target = target;
        nTarget.time = time;
        nTarget.interpolationType = interpolationType;
        nTarget.customInterpolator = customInterpolator;
        nTarget.customParams = customParams;
        nTarget.easeStrength = easeStrength;
        nTarget.initTime = Time.time;

        if (attribute == Attribute.POSITION) { nTarget.init = toChange.position; }
        else if (attribute == Attribute.SCALE) { nTarget.init = toChange.localScale; }
        else if (attribute == Attribute.ROTATION) { nTarget.init = toChange.rotation.eulerAngles; }
        targets.Add(nTarget);

        int id = UnityEngine.Random.Range(int.MinValue, int.MaxValue);
        while (active.Contains(id))
        {
            id = UnityEngine.Random.Range(int.MinValue, int.MaxValue);
        }

        return id;
    }

    public static bool IsActive(int id)
    {
        if (active.Contains(id))
        {
            return true;
        }
        return false;
    }
    
    void Update()
    {
        for (int i = 0; i < targets.Count; i++)
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
                targets.RemoveAt(i--);
            }
        }
    }

}
