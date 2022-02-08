using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
public class PrismManager : MonoBehaviour
{
    public int prismCount = 10;
    public float prismRegionRadiusXZ = 5;
    public float prismRegionRadiusY = 5;
    public float maxPrismScaleXZ = 5;
    public float maxPrismScaleY = 5;
    public GameObject regularPrismPrefab;
    public GameObject irregularPrismPrefab;

    private List<Prism> prisms = new List<Prism>();
    private List<GameObject> prismObjects = new List<GameObject>();
    private GameObject prismParent;
    private Dictionary<Prism, bool> prismColliding = new Dictionary<Prism, bool>();

    private const float UPDATE_RATE = 0.5f;

    #region Unity Functions

    void Start()
    {
        Random.InitState(0);    //10 for no collision

        prismParent = GameObject.Find("Prisms");
        for (int i = 0; i < prismCount; i++)
        {
            var randPointCount = Mathf.RoundToInt(3 + Random.value * 7);
            var randYRot = Random.value * 360;
            var randScale = new Vector3((Random.value - 0.5f) * 2 * maxPrismScaleXZ, (Random.value - 0.5f) * 2 * maxPrismScaleY, (Random.value - 0.5f) * 2 * maxPrismScaleXZ);
            var randPos = new Vector3((Random.value - 0.5f) * 2 * prismRegionRadiusXZ, (Random.value - 0.5f) * 2 * prismRegionRadiusY, (Random.value - 0.5f) * 2 * prismRegionRadiusXZ);

            GameObject prism = null;
            Prism prismScript = null;
            if (Random.value < 0.5f)
            {
                prism = Instantiate(regularPrismPrefab, randPos, Quaternion.Euler(0, randYRot, 0));
                prismScript = prism.GetComponent<RegularPrism>();
            }
            else
            {
                prism = Instantiate(irregularPrismPrefab, randPos, Quaternion.Euler(0, randYRot, 0));
                prismScript = prism.GetComponent<IrregularPrism>();
            }
            prism.name = "Prism " + i;
            prism.transform.localScale = randScale;
            prism.transform.parent = prismParent.transform;
            prismScript.pointCount = randPointCount;
            prismScript.prismObject = prism;

            prisms.Add(prismScript);
            prismObjects.Add(prism);
            prismColliding.Add(prismScript, false);
        }

        StartCoroutine(Run());
    }

    void Update()
    {
        #region Visualization

        DrawPrismRegion();
        DrawPrismWireFrames();

#if UNITY_EDITOR
        if (Application.isFocused)
        {
            UnityEditor.SceneView.FocusWindowIfItsOpen(typeof(UnityEditor.SceneView));
        }
#endif

        #endregion
    }

    IEnumerator Run()
    {
        yield return null;

        while (true)
        {
            foreach (var prism in prisms)
            {
                prismColliding[prism] = false;
            }

            foreach (var collision in PotentialCollisions())
            {
                if (CheckCollision(collision))
                {
                    prismColliding[collision.a] = true;
                    prismColliding[collision.b] = true;

                    ResolveCollision(collision);
                }
            }

            yield return new WaitForSeconds(UPDATE_RATE);
        }
    }

    #endregion

    #region Incomplete Functions
    private class Point
    {
        public float x;
        public float y;
    }
    private class AABB
    {
        public Point min;
        public Point max;
        public Prism PrismObject;
    }

    private class Pair
    {
        public AABB bbox;
        public float key;
    }

    private class minHeap
    {
        public Pair[] contained;
        public int sizeHeap;
        private int alloced;
        public minHeap()
        {
            sizeHeap = 0;
            alloced = 20;
            contained = new Pair[alloced];
        }
        public Pair removeMin()
        {
            if (sizeHeap <= 0)
            {
                return null;
            }
            if (sizeHeap == 1)
            {
                sizeHeap--;
                return contained[0];
            }

            Pair root = contained[0];
            contained[0] = contained[sizeHeap - 1];
            sizeHeap--;
            heapify(0);
            return root;
        }

        public Pair getMin()
        {
            return contained[0];
        }

        private void heapify(int i)
        {
            int l = left(i);
            int r = right(i);
            int smallest = i;
            if (l < sizeHeap && contained[l].key < contained[i].key)
            {
                smallest = l;
            }
            if (r < sizeHeap && contained[r].key < contained[i].key)
            {
                smallest = r;
            }
            if (smallest != i)
            {
                swap(i, smallest);
                heapify(smallest);
            }
        }
        private void swap(int x, int y)
        {
            Pair temp = contained[x];
            contained[x] = contained[y];
            contained[y] = temp;
        }

        private int left(int i)
        {
            return i * 2 + 1;
        }
        private int right(int i)
        {
            return i * 2 + 2;
        }

        private void expand()
        {
            alloced = 2 * alloced;
            Pair[] temp = new Pair[alloced];
            for (int i = 0; i < contained.Length; i++)
            {
                temp[i] = contained[i];
            }
            contained = temp;

        }

        public void addPair(AABB addition, float key)
        {

            sizeHeap++;
            if (sizeHeap >= alloced)
            {
                expand();
            }
            Pair entry = new Pair();
            entry.bbox = addition;
            entry.key = key;
            contained[sizeHeap - 1] = entry;
            int val = sizeHeap - 1;
            while (val != 0 && entry.key < contained[val / 2].key)
            {
                swap(val, val / 2);
                val = val / 2;
            }
        }

    }

    private IEnumerable<PrismCollision> PotentialCollisions()
    {
        //Create list of bounding boxes (might be better to make the min/max points inherit to the Prism class)

        SortedList<float, AABB> boundingBoxes = new SortedList<float, AABB>();
        for (int i = 0; i < prisms.Count; i++)
        {
            Point Max = new Point
            {
                //Also better to run the GetComponent at the creation of a prism class so it doesnt have to be re-run every frame
                x = prisms[i].GetComponent<Renderer>().bounds.max.x,
                y = prisms[i].GetComponent<Renderer>().bounds.max.y,

            };
            Point Min = new Point
            {
                x = prisms[i].GetComponent<Renderer>().bounds.min.x,
                y = prisms[i].GetComponent<Renderer>().bounds.min.y,
            };
            AABB boundingBox = new AABB { max = Max, min = Min, PrismObject = prisms[i] };
            boundingBoxes.Add(Min.x, boundingBox);
        }

        // Initialize Stack TODO: Create MinHeap Stack
        minHeap currentCollisions = new minHeap();

        // Initialize all Potential collisions
        List<PrismCollision> collisions = new List<PrismCollision>();

        // Loop through the sorted Bounding Boxes
        foreach (KeyValuePair<float, AABB> kvp in boundingBoxes)
        {
            // Using a sorted stack to keep track of potentially intersecting objects

            /* Remove each object from the stack that ends befor the currend bounding box starts
            while (currentCollisions.sizeHeap > 0 && kvp.Value.min.x > currentCollisions.getMin().key)
            {
                currentCollisions.removeMin();
            }
            */
            // For each object in the stack, add it as a potential colision
            for (int i = 0; i < currentCollisions.sizeHeap; i++)
            {
                Pair collision = currentCollisions.contained[i];

                collisions.Add(new PrismCollision
                {
                    a = kvp.Value.PrismObject,
                    b = collision.bbox.PrismObject,
                });
            }

            // Add current bounding box to the stack for future bounding boxes to check against
            currentCollisions.addPair(kvp.Value, kvp.Value.max.x);
        }

        return collisions;
    }

    /*
        This function will get the farthest point in the Prism x that is in direction d.
        The function loops through each point in Prism X and calculates the dot product
        against direction.  If the point is the largest it saves that point and returns it.
    */
    private Vector3 GetFarthestPoint(Prism x, Vector3 d)
    {
        float largestDistance = -1000000000000f;
        Vector3 farthestPoint = Vector3.zero;
        foreach (Vector3 point in x.points)
        {
            float newDistance = Vector3.Dot(point, d);
            if (newDistance > largestDistance)
            {
                largestDistance = newDistance;
                farthestPoint = point;
            }
        }
        return farthestPoint;
    }
    /*
        The support function gets the farthest point in both directions d for each Prism
        and returns the difference between them.
    */
    private Vector3 support(Prism a, Prism b, Vector3 d)
    {
        Vector3 point1 = GetFarthestPoint(a, d);
        Vector3 point2 = GetFarthestPoint(b, -d);
        return (point1 - point2);
    }

    /*
        Checks different edges in 3D Triangle to see if the edges enclose the origin
        or if not change direction.
    */
    private bool Check3DTriangle(ref List<Vector3> simplex, ref Vector3 direction){
        Vector3 a = simplex[3];
        Vector3 ao = -a;
        Vector3 b = simplex[2];
        Vector3 c = simplex[1];
        Vector3 d = simplex[0];
        Vector3 ab = b - a;
        Vector3 ac = c - a;
        Vector3 ad = d - a;

        Vector3 abc = Vector3.Cross(ab, ac);
        Vector3 acd = Vector3.Cross(ac, ad);
        Vector3 adb = Vector3.Cross(ad, ab);
        
        if(Vector3.Dot(abc, ao) > 0){
            simplex.RemoveAt(0);
            return CheckTriangle(ref simplex, ref direction);
        }
        if(Vector3.Dot(acd, ao) > 0){
            simplex.RemoveAt(2);
            return CheckTriangle(ref simplex, ref direction);
        }
        if(Vector3.Dot(adb, ao) > 0){
            simplex.RemoveAt(1);
            simplex[1] = d;
            simplex[0] = b;
            return CheckTriangle(ref simplex, ref direction);
        }

        return true;
    }

    /*
        Checks to see if the edges ab and ac enclose the origin.
        Depending on direction towards origin changes direction or returns true if 
        the edges do enclose origin.
    */
    private bool CheckTriangle(ref List<Vector3> simplex, ref Vector3 d){
        Vector3 a = simplex[0];
        Vector3 b = simplex[1];
        Vector3 c = simplex[2];
        Vector3 ao = -a;
        Vector3 ab = b - a;
        Vector3 ac = c - a;
        Vector3 abc = Vector3.Cross(ab,ac);
        
        if (Vector3.Dot(Vector3.Cross(abc,ac), ao) > 0)
        {
            if(Vector3.Dot(ac, ao) > 0){
                simplex.RemoveAt(1);
                d = Vector3.Cross(Vector3.Cross(ac,ao),ac).normalized;
            }else{
                simplex.RemoveAt(2);
                return CheckLine(ref simplex, ref d);
            }
        }
        else
        {
            if (Vector3.Dot(Vector3.Cross(ab,abc), ao) > 0)
            {
                simplex.RemoveAt(2);
                return CheckLine(ref simplex, ref d);
            }
            else
            {   
                /*
                //Doesn't work but would be for 3D GJK.
                if(Vector3.Dot(abc,ao) > 0){
                    d = abc;
                }else{
                    simplex[1] = c;
                    simplex[2] = b;
                    d = -abc;
                }*/
                return true;
            }
        }
        return false;
    }

    /*
        If there are only two points in the simplex, will change the direction
        depending on if dot product of edge AB is towards the origin.
        If not towards origin will remove point B and go in opposite direction to a.
    */
    private bool CheckLine(ref List<Vector3> simplex, ref Vector3 d){
        Vector3 a = simplex[0];
        Vector3 ao = -a;
        Vector3 b = simplex[1];
        Vector3 ab = b - a;
        if(Vector3.Dot(ab, ao) > 0){
            d = Vector3.Cross(Vector3.Cross(ab, ao), ab).normalized;
        }else{
            simplex.RemoveAt(1);
            d = ao.normalized;
        }

        return false;
    }

    /*
        The function checks to see if the simplex that was created is over the origin space.
        It checks if the Simplex is a Line or Triangle or 3D Triangle by number of points in simplex.
        If it is a triangle, it checks if the origin point is within the Triangle.
        If not than it removes a point from the Simplex and changes the direction.
        If it is a Line, it finds a new direction for d
    */
    private bool IsOrigin(ref List<Vector3> simplex, ref Vector3 d)
    {
        if(simplex.Count >= 4){
            return Check3DTriangle(ref simplex, ref d);
        }
        if (simplex.Count == 3)
        {
            return CheckTriangle(ref simplex, ref d);
        }
        if(simplex.Count == 2)
        {
            return CheckLine(ref simplex, ref d);
        }
        if(simplex.Count == 1){
            d = -simplex[0].normalized;
        }
        return false;
    }

    /*
        The function finds the closest edge from the origin and returns the depth penetration vector,
        distance and position.
    */
    private void ClosestEdge(ref float distance, ref Vector3 normal, ref int pos, List<Vector3> Simplex){
        distance = Mathf.Infinity;
        int i = 0;
        foreach(Vector3 a in Simplex){
            int j = i + 1;
            i++;
            if(j >= Simplex.Count)
                j = 0;
            
            Vector3 b = Simplex[j];
            Vector3 e = b - a;
            Vector3 newNormal = Vector3.Cross(Vector3.Cross(e, a), e).normalized;
            float newDistance = Vector3.Dot(newNormal, a);
            if(newDistance < distance){
                distance = newDistance;
                normal = newNormal;
                pos = j;
            }
        }
    }

    /*
        Function will run the GJK Algorithm between two Prisms A & B.
        If the Prisms are colliding, it will return true else false.
        If the Prisms are colliding, it will also run the EPA algorithm,
        and store the penetration depth vector.
    */
    private bool CheckCollision(PrismCollision collision)
    {
        var prismA = collision.a;
        var prismB = collision.b;

        bool isCollision = false;
        List<Vector3> simplex = new List<Vector3>();
        Vector3 direction = new Vector3(1, 0, 0);
        Vector3 supportVec = support(prismA, prismB, direction);
        simplex.Add(supportVec);
        direction = -supportVec;

        while (true)
        {   
            supportVec = support(prismA, prismB, direction);
            if (Vector3.Dot(supportVec, direction) <= 0)
            {
                return false;
            }
            simplex.Add(supportVec);
            if(IsOrigin(ref simplex, ref direction)){
                isCollision = true;
                break;
            }
        }
        
        float distance = 0;
        Vector3 normal = Vector3.zero;
        int pos = 0;
        while(true){
            ClosestEdge(ref distance, ref normal, ref pos, simplex);
            Vector3 p = support(prismA, prismB, normal);
            float d = Vector3.Dot(p,normal);
            if(d - distance < 0.000001){
                collision.penetrationDepthVectorAB = normal;
                break;
            }else{
                simplex.Insert(pos, p);
            }
        }

        return isCollision;
    }

    #endregion

    #region Private Functions

    private void ResolveCollision(PrismCollision collision)
    {
        var prismObjA = collision.a.prismObject;
        var prismObjB = collision.b.prismObject;

        var pushA = -collision.penetrationDepthVectorAB / 2;
        var pushB = collision.penetrationDepthVectorAB / 2;

        for (int i = 0; i < collision.a.pointCount; i++)
        {
            collision.a.points[i] += pushA;
        }
        for (int i = 0; i < collision.b.pointCount; i++)
        {
            collision.b.points[i] += pushB;
        }
        //prismObjA.transform.position += pushA;
        //prismObjB.transform.position += pushB;

        Debug.DrawLine(prismObjA.transform.position, prismObjA.transform.position + collision.penetrationDepthVectorAB, Color.cyan, UPDATE_RATE);
    }

    #endregion

    #region Visualization Functions

    private void DrawPrismRegion()
    {
        var points = new Vector3[] { new Vector3(1, 0, 1), new Vector3(1, 0, -1), new Vector3(-1, 0, -1), new Vector3(-1, 0, 1) }.Select(p => p * prismRegionRadiusXZ).ToArray();

        var yMin = -prismRegionRadiusY;
        var yMax = prismRegionRadiusY;

        var wireFrameColor = Color.yellow;

        foreach (var point in points)
        {
            Debug.DrawLine(point + Vector3.up * yMin, point + Vector3.up * yMax, wireFrameColor);
        }

        for (int i = 0; i < points.Length; i++)
        {
            Debug.DrawLine(points[i] + Vector3.up * yMin, points[(i + 1) % points.Length] + Vector3.up * yMin, wireFrameColor);
            Debug.DrawLine(points[i] + Vector3.up * yMax, points[(i + 1) % points.Length] + Vector3.up * yMax, wireFrameColor);
        }
    }

    private void DrawPrismWireFrames()
    {
        for (int prismIndex = 0; prismIndex < prisms.Count; prismIndex++)
        {
            var prism = prisms[prismIndex];
            var prismTransform = prismObjects[prismIndex].transform;

            var yMin = prism.midY - prism.height / 2 * prismTransform.localScale.y;
            var yMax = prism.midY + prism.height / 2 * prismTransform.localScale.y;

            var wireFrameColor = prismColliding[prisms[prismIndex]] ? Color.red : Color.green;

            foreach (var point in prism.points)
            {
                Debug.DrawLine(point + Vector3.up * yMin, point + Vector3.up * yMax, wireFrameColor);
            }

            for (int i = 0; i < prism.pointCount; i++)
            {
                Debug.DrawLine(prism.points[i] + Vector3.up * yMin, prism.points[(i + 1) % prism.pointCount] + Vector3.up * yMin, wireFrameColor);
                Debug.DrawLine(prism.points[i] + Vector3.up * yMax, prism.points[(i + 1) % prism.pointCount] + Vector3.up * yMax, wireFrameColor);
            }
        }
    }

    #endregion

    #region Utility Classes

    private class PrismCollision
    {
        public Prism a;
        public Prism b;
        public Vector3 penetrationDepthVectorAB;
    }

    private class Tuple<K, V>
    {
        public K Item1;
        public V Item2;

        public Tuple(K k, V v)
        {
            Item1 = k;
            Item2 = v;
        }
    }

    #endregion
}
