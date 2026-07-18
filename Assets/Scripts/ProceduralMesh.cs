using System.Collections.Generic;
using UnityEngine;

namespace DoorPuzzle
{
    public static class ProceduralMesh
    {
        private static Mesh sharedChamferedCube;

        public static Mesh SharedChamferedCube
        {
            get
            {
                if (sharedChamferedCube == null) sharedChamferedCube = CreateChamferedCube(0.075f);
                return sharedChamferedCube;
            }
        }

        public static Mesh CreateTorusArc(float majorRadius, float tubeRadius, float startDegrees, float sweepDegrees, int arcSegments = 40, int tubeSegments = 8)
        {
            var vertices = new List<Vector3>((arcSegments + 1) * (tubeSegments + 1));
            var normals = new List<Vector3>(vertices.Capacity);
            var colors = new List<Color>(vertices.Capacity);
            var uvs = new List<Vector2>(vertices.Capacity);
            var triangles = new List<int>(arcSegments * tubeSegments * 6);

            for (var arc = 0; arc <= arcSegments; arc++)
            {
                var arcT = arc / (float)arcSegments;
                var angle = Mathf.Deg2Rad * (startDegrees + sweepDegrees * arcT);
                var radial = new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0f);
                var center = radial * majorRadius;

                for (var tube = 0; tube <= tubeSegments; tube++)
                {
                    var tubeT = tube / (float)tubeSegments;
                    var tubeAngle = tubeT * Mathf.PI * 2f;
                    var normal = radial * Mathf.Cos(tubeAngle) + Vector3.forward * Mathf.Sin(tubeAngle);
                    vertices.Add(center + normal * tubeRadius);
                    normals.Add(normal.normalized);
                    colors.Add(Color.Lerp(new Color(0.35f, 0.85f, 1f, 0.75f), new Color(1f, 0.65f, 0.2f, 0.9f), arcT));
                    // Mesh VFX should stay fully opaque in the shader's optional
                    // radial particle mask, so every torus vertex sits at UV center.
                    uvs.Add(Vector2.one * 0.5f);
                }
            }

            var row = tubeSegments + 1;
            for (var arc = 0; arc < arcSegments; arc++)
            {
                for (var tube = 0; tube < tubeSegments; tube++)
                {
                    var index = arc * row + tube;
                    triangles.Add(index);
                    triangles.Add(index + row);
                    triangles.Add(index + 1);
                    triangles.Add(index + 1);
                    triangles.Add(index + row);
                    triangles.Add(index + row + 1);
                }
            }

            var mesh = new Mesh { name = "Echo Sigil Arc" };
            mesh.SetVertices(vertices);
            mesh.SetNormals(normals);
            mesh.SetColors(colors);
            mesh.SetUVs(0, uvs);
            mesh.SetTriangles(triangles, 0);
            mesh.RecalculateBounds();
            return mesh;
        }

        public static Mesh CreateChamferedCube(float bevel)
        {
            const float half = 0.5f;
            bevel = Mathf.Clamp(bevel, 0.005f, 0.24f);
            var inset = half - bevel;
            var vertices = new List<Vector3>(96);
            var normals = new List<Vector3>(96);
            var uvs = new List<Vector2>(96);
            var triangles = new List<int>(132);

            // Six broad faces.
            AddQuad(vertices, normals, uvs, triangles,
                new Vector3(half, -inset, -inset), new Vector3(half, -inset, inset),
                new Vector3(half, inset, inset), new Vector3(half, inset, -inset), Vector3.right);
            AddQuad(vertices, normals, uvs, triangles,
                new Vector3(-half, -inset, inset), new Vector3(-half, -inset, -inset),
                new Vector3(-half, inset, -inset), new Vector3(-half, inset, inset), Vector3.left);
            AddQuad(vertices, normals, uvs, triangles,
                new Vector3(-inset, half, -inset), new Vector3(inset, half, -inset),
                new Vector3(inset, half, inset), new Vector3(-inset, half, inset), Vector3.up);
            AddQuad(vertices, normals, uvs, triangles,
                new Vector3(-inset, -half, inset), new Vector3(inset, -half, inset),
                new Vector3(inset, -half, -inset), new Vector3(-inset, -half, -inset), Vector3.down);
            AddQuad(vertices, normals, uvs, triangles,
                new Vector3(-inset, -inset, half), new Vector3(-inset, inset, half),
                new Vector3(inset, inset, half), new Vector3(inset, -inset, half), Vector3.forward);
            AddQuad(vertices, normals, uvs, triangles,
                new Vector3(inset, -inset, -half), new Vector3(inset, inset, -half),
                new Vector3(-inset, inset, -half), new Vector3(-inset, -inset, -half), Vector3.back);

            // Twelve chamfered edges.
            for (var sy = -1; sy <= 1; sy += 2)
            for (var sz = -1; sz <= 1; sz += 2)
            {
                AddQuad(vertices, normals, uvs, triangles,
                    new Vector3(-half, sy * half, sz * inset), new Vector3(half, sy * half, sz * inset),
                    new Vector3(half, sy * inset, sz * half), new Vector3(-half, sy * inset, sz * half),
                    new Vector3(0f, sy, sz).normalized);
            }

            for (var sx = -1; sx <= 1; sx += 2)
            for (var sz = -1; sz <= 1; sz += 2)
            {
                AddQuad(vertices, normals, uvs, triangles,
                    new Vector3(sx * half, -half, sz * inset), new Vector3(sx * inset, -half, sz * half),
                    new Vector3(sx * inset, half, sz * half), new Vector3(sx * half, half, sz * inset),
                    new Vector3(sx, 0f, sz).normalized);
            }

            for (var sx = -1; sx <= 1; sx += 2)
            for (var sy = -1; sy <= 1; sy += 2)
            {
                AddQuad(vertices, normals, uvs, triangles,
                    new Vector3(sx * half, sy * inset, -half), new Vector3(sx * half, sy * inset, half),
                    new Vector3(sx * inset, sy * half, half), new Vector3(sx * inset, sy * half, -half),
                    new Vector3(sx, sy, 0f).normalized);
            }

            // Eight small corner faces complete the silhouette.
            for (var sx = -1; sx <= 1; sx += 2)
            for (var sy = -1; sy <= 1; sy += 2)
            for (var sz = -1; sz <= 1; sz += 2)
            {
                AddTriangle(vertices, normals, uvs, triangles,
                    new Vector3(sx * half, sy * inset, sz * inset),
                    new Vector3(sx * inset, sy * half, sz * inset),
                    new Vector3(sx * inset, sy * inset, sz * half),
                    new Vector3(sx, sy, sz).normalized);
            }

            var mesh = new Mesh { name = "Shared Chamfered Cube" };
            mesh.SetVertices(vertices);
            mesh.SetNormals(normals);
            mesh.SetUVs(0, uvs);
            mesh.SetTriangles(triangles, 0);
            mesh.RecalculateBounds();
            return mesh;
        }

        private static void AddQuad(List<Vector3> vertices, List<Vector3> normals, List<Vector2> uvs,
            List<int> triangles, Vector3 a, Vector3 b, Vector3 c, Vector3 d, Vector3 normal)
        {
            var start = vertices.Count;
            vertices.Add(a);
            vertices.Add(b);
            vertices.Add(c);
            vertices.Add(d);
            for (var i = 0; i < 4; i++) normals.Add(normal);
            uvs.Add(new Vector2(0f, 0f));
            uvs.Add(new Vector2(1f, 0f));
            uvs.Add(new Vector2(1f, 1f));
            uvs.Add(new Vector2(0f, 1f));

            var forward = Vector3.Dot(Vector3.Cross(b - a, c - a), normal) >= 0f;
            if (forward)
            {
                triangles.Add(start); triangles.Add(start + 1); triangles.Add(start + 2);
                triangles.Add(start); triangles.Add(start + 2); triangles.Add(start + 3);
            }
            else
            {
                triangles.Add(start); triangles.Add(start + 2); triangles.Add(start + 1);
                triangles.Add(start); triangles.Add(start + 3); triangles.Add(start + 2);
            }
        }

        private static void AddTriangle(List<Vector3> vertices, List<Vector3> normals, List<Vector2> uvs,
            List<int> triangles, Vector3 a, Vector3 b, Vector3 c, Vector3 normal)
        {
            var start = vertices.Count;
            vertices.Add(a);
            vertices.Add(b);
            vertices.Add(c);
            normals.Add(normal);
            normals.Add(normal);
            normals.Add(normal);
            uvs.Add(new Vector2(0f, 0f));
            uvs.Add(new Vector2(1f, 0f));
            uvs.Add(new Vector2(0.5f, 1f));
            var forward = Vector3.Dot(Vector3.Cross(b - a, c - a), normal) >= 0f;
            triangles.Add(start);
            triangles.Add(start + (forward ? 1 : 2));
            triangles.Add(start + (forward ? 2 : 1));
        }
    }
}
