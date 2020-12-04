using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using RapidGUI;

public class Interface2D : MonoBehaviour
{
    public GameObject pointPrefab;
    public GameObject centerPrefab;
    public Color lineColor;

    private Rect windowRect = new Rect(0, 0, 250, 250);
    private List<Triangle> tris;
    private List<Vector3> pointsCloud3D;
    private int verticesAmount = 10;
    private Material lineMat;
    private List<Edge> edges;

    void Start() {
        tris = new List<Triangle>();
        pointsCloud3D = new List<Vector3>();
        lineMat = new Material(Shader.Find("Unlit/Color"));
        lineMat.color = lineColor;
        edges = new List<Edge>();
    }

    private void OnGUI() {
        windowRect = GUI.ModalWindow(GetHashCode(), windowRect, DoGUI, "Actions", RGUIStyle.darkWindow);
    }

    public void DoGUI(int windowID) {
        verticesAmount = RGUI.Field(verticesAmount, "Number of Points");

        if (GUILayout.Button("Generate 2D Points Cloud")) {
            ResetScene();
            ResetData();
            pointsCloud3D = GenerateRandomVertices(verticesAmount);
            GeneratePoints(pointPrefab, pointsCloud3D);
        }

        if (ValidCloudPoint()) {
            GUILayout.Label("Triangulation");

            if (GUILayout.Button("Direct Delaunay")) {
                pointsCloud3D = UpdateVertices();
                ResetMesh();
                ResetData();
                ResetCenter();

                // Convertion
                List<Vector2> pointsCloud2D = ConvertListVector3ToVector2(pointsCloud3D);
                
                Delaunay2D.Delaunay(pointsCloud2D, ref tris);

                GenerateMeshIndirect(tris);
            }

            if (GUILayout.Button("Direct Regular")) {
                pointsCloud3D = UpdateVertices();
                ResetMesh();
                ResetData();
                ResetCenter();

                // Convertion
                List<Vector2> pointsCloud2D = ConvertListVector3ToVector2(pointsCloud3D);
                
                Delaunay2D.Regular(pointsCloud2D, ref tris);

                GenerateMeshIndirect(tris);
            }

            if (tris.Count > 0) {
                if (GUILayout.Button("Flip")) {
                    Delaunay2D.FlipToDelaunay(ref tris);
                    GenerateMeshIndirect(tris);
                }

                if (GUILayout.Button("Direct Voronoi2D")) {

                    List<Vector2> AllCenterPoints;
                    AllCenterPoints = Delaunay2D.AllCenterPoint(ref tris);
                    GenerateCenter(centerPrefab, AllCenterPoints);

                    edges = Delaunay2D.Voronoi2D(ref tris);
                }
            }
        }
    }

    void OnPostRender() {
        foreach (var edge in edges) {
            GL.Begin(GL.LINES);
            lineMat.SetPass(0);
            GL.Color(new Color(lineMat.color.r, lineMat.color.g, lineMat.color.b, lineMat.color.a));
            GL.Vertex3(edge.start.x, edge.start.y, 0f);
            GL.Vertex3(edge.end.x, edge.end.y, 0f);
            GL.End();
        }
    }

    bool ValidCloudPoint() {
        return pointsCloud3D.Count > 0;
    }

    public void ResetData() {
        tris.Clear();
        edges.Clear();
    }

    static public void ResetScene() {
        ResetPoint();
        ResetCenter();
        ResetMesh();
    }

    static public void ResetPoint() {
        GameObject tmp = GameObject.Find("Point(Clone)");
        while(tmp != null) {
            DestroyImmediate(tmp);
            tmp = GameObject.Find("Point(Clone)");
        }
    }

    static public void ResetCenter() {
        GameObject tmp = GameObject.Find("Center(Clone)");
        while(tmp != null) {
            DestroyImmediate(tmp);
            tmp = GameObject.Find("Center(Clone)");
        }
    }

    static public void ResetMesh() {
        GameObject thisBuilding = GameObject.Find("Building");
        if (thisBuilding != null) {
            DestroyImmediate(thisBuilding);
        }
    }

    static public void GeneratePoints(GameObject prefab, List<Vector3> vertices) {
		for (int i = 0; i < vertices.Count; i++) {
			Instantiate(prefab, vertices[i], Quaternion.identity);
		}
    }

    static public void GenerateCenter(GameObject prefab, List<Vector2> vertices)
    {
        for (int i = 0; i < vertices.Count; i++)
        {
            Instantiate(prefab, vertices[i], Quaternion.identity);
        }
    }

    static public List<Vector3> UpdateVertices() {
        List<Vector3> newPoints3D = new List<Vector3>();
        GameObject[] allGOs = FindObjectsOfType<GameObject>();
        foreach(var go in allGOs) {
            if (go.name == "Point(Clone)") {
                newPoints3D.Add(go.transform.position);
            }
        }
        return newPoints3D;
    }

    static List<Vector3> GenerateRandomVertices(int verticesAmount) {
        Vector3 uperLeftCorner = Camera.main.ScreenToWorldPoint(new Vector3(100, 100, 10)); // -10.0f if bugs
        Vector3 lowerRightCorner = Camera.main.ScreenToWorldPoint(new Vector3(Screen.width - 100, Screen.height - 100, 10)); // -10.0f if bugs
        
        List<Vector3> points3D = new List<Vector3>();
        for (int i = 0; i < verticesAmount; i++) {
            points3D.Add(new Vector3(
                UnityEngine.Random.Range(uperLeftCorner.x, lowerRightCorner.x),
                UnityEngine.Random.Range(uperLeftCorner.y, lowerRightCorner.y),
                0
            ));
        }

        return points3D;
    }

    static public void SortInClockWise(ref List<Vector2> points2D) {
        ConvexHull.SortByAngle(ref points2D, ConvexHull.GetBarycenter(points2D));
        points2D.Reverse();
    }
        

    static public void SortInClockWise(ref List<Vector3> points3D) {
        List<Vector2> points2D = ConvertListVector3ToVector2(points3D);
        SortInClockWise(ref points2D);
        points3D = ConvertListVector2ToVector3(points2D); 
    }

    static List<Vector3> ConvertListVector2ToVector3(List<Vector2> points2D) {
        List<Vector3> points3D = new List<Vector3>();
        for (int i = 0; i < points2D.Count; i++) {
            points3D.Add(points2D[i]);
        }
        return points3D;
    }

    static List<Vector2> ConvertListVector3ToVector2(List<Vector3> points3D) {
        List<Vector2> points2D = new List<Vector2>();
        for (int i = 0; i < points3D.Count; i++) {
            points2D.Add(points3D[i]);
        }
        return points2D;
    }

    static public void GenerateMeshIndirect(List<Triangle> triangles) {
        triangles.ForEach(tri => SortInClockWise(ref tri.vertices));

        List<Vector2> points2D = new List<Vector2>();
        List<int> indices = new List<int>();
        
        for(int i = 0; i < triangles.Count; i++) {
            for(var j = 0; j < 3; j++) {
                points2D.Add(triangles[i].vertices[j]);
                indices.Add(i * 3 + j);
            }
        }

        GenerateMesh(ConvertListVector2ToVector3(points2D), indices);
    }

    static public void GenerateMesh(List<Vector3> vertices, List<int> indices) {
        GameObject thisBuilding = GameObject.Find("Building");
        if (thisBuilding == null) {
            // Create a building game object
            thisBuilding = new GameObject ("Building");
        }

        var normals = new List<Vector3>();
        
        for (int i = 0; i < vertices.Count; i++) {
            normals.Add(Vector3.back);
        }

        // Create and apply the mesh
        MeshFilter mf = thisBuilding.GetComponent<MeshFilter>();
        if (mf == null) {
            // Create and apply the mesh
            mf = thisBuilding.AddComponent<MeshFilter>();
        }

        Mesh mesh = new Mesh();
        mf.mesh = mesh;

        mesh.SetVertices(vertices);
        mesh.SetNormals(normals);
        mesh.SetTriangles(indices, 0);

        mesh.RecalculateBounds();
        mesh.RecalculateNormals();

        MeshRenderer rend = thisBuilding.GetComponent<MeshRenderer>();
        if (rend == null) {
            rend = thisBuilding.AddComponent<MeshRenderer>();
        }
        rend.material = new Material(Shader.Find("SuperSystems/Wireframe-Shaded-Unlit"));
    }
}