using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class IrregularPrism : Prism
{

    void Start()
    {
        pointCount = Mathf.Max(pointCount, 3);
        points = Enumerable.Range(0, pointCount).Select(i => Random.value).OrderBy(i => i).Select(theta => Quaternion.AngleAxis(360f * theta, Vector3.up) * Vector3.forward * 0.5f).ToArray();
        points = points.Select(p => transform.position + Quaternion.AngleAxis(transform.eulerAngles.y, Vector3.up) * new Vector3(p.x * transform.localScale.x, 0, p.z * transform.localScale.z)).ToArray();
        midY = 0;
        height = 2;
    }
    
}
