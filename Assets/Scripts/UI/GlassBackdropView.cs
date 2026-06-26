using UnityEngine;
using UnityEngine.UI;

namespace PeopleOfMath.UI
{
    [DisallowMultipleComponent]
    public class GlassBackdropView : MonoBehaviour
    {
        static Texture2D _gradientTexture;
        static Texture2D _softCircleTexture;

        [SerializeField] RawImage gradientImage;
        [SerializeField] RawImage blob1;
        [SerializeField] RawImage blob2;
        [SerializeField] RawImage blob3;

        public static Texture2D GradientTexture => _gradientTexture ??= CreateGradientTexture();
        public static Texture2D SoftCircleTexture => _softCircleTexture ??= CreateSoftCircleTexture();

        void Awake()
        {
            EnsureHierarchy();
            ApplyVisuals();
        }

        void EnsureHierarchy()
        {
            if (gradientImage == null)
            {
                var gradientGo = transform.Find("Gradient");
                if (gradientGo == null)
                {
                    gradientGo = CreateStretchChild(transform, "Gradient").transform;
                    gradientImage = gradientGo.GetComponent<RawImage>();
                }
                else
                {
                    gradientImage = gradientGo.GetComponent<RawImage>();
                }
            }

            blob1 = EnsureBlob(blob1, "Blob1", new Vector2(0.15f, 0.72f), new Vector2(520f, 520f), new Color(1f, 0.624f, 0.263f, 0.55f));
            blob2 = EnsureBlob(blob2, "Blob2", new Vector2(0.82f, 0.58f), new Vector2(480f, 480f), new Color(0.749f, 0.353f, 0.949f, 0.50f));
            blob3 = EnsureBlob(blob3, "Blob3", new Vector2(0.55f, 0.28f), new Vector2(640f, 640f), new Color(0.420f, 0.188f, 0.659f, 0.45f));
        }

        static GameObject CreateStretchChild(Transform parent, string name)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(RawImage));
            go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            return go;
        }

        RawImage EnsureBlob(RawImage existing, string name, Vector2 anchor, Vector2 size, Color color)
        {
            if (existing != null)
                return existing;

            var blobTransform = transform.Find(name);
            GameObject blobGo;
            if (blobTransform == null)
            {
                blobGo = new GameObject(name, typeof(RectTransform), typeof(RawImage));
                blobGo.transform.SetParent(transform, false);
            }
            else
            {
                blobGo = blobTransform.gameObject;
            }

            var rt = blobGo.GetComponent<RectTransform>();
            rt.anchorMin = anchor;
            rt.anchorMax = anchor;
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = Vector2.zero;
            rt.sizeDelta = size;

            var image = blobGo.GetComponent<RawImage>();
            image.raycastTarget = false;
            image.color = color;
            return image;
        }

        public void ApplyVisuals()
        {
            EnsureHierarchy();

            if (gradientImage != null)
            {
                gradientImage.raycastTarget = false;
                gradientImage.texture = GradientTexture;
                gradientImage.color = Color.white;
            }

            ApplyBlob(blob1, new Color(1f, 0.624f, 0.263f, 0.55f));
            ApplyBlob(blob2, new Color(0.749f, 0.353f, 0.949f, 0.50f));
            ApplyBlob(blob3, new Color(0.420f, 0.188f, 0.659f, 0.45f));
        }

        static void ApplyBlob(RawImage blob, Color color)
        {
            if (blob == null)
                return;

            blob.raycastTarget = false;
            blob.texture = SoftCircleTexture;
            blob.color = color;
        }

        public void BlitTo(RenderTexture target)
        {
            var active = RenderTexture.active;
            RenderTexture.active = target;
            GL.Clear(true, true, UiTheme.CameraBackground);

            BlitTexture(target, GradientTexture, new Rect(0f, 0f, 1f, 1f), Color.white);

            BlitBlobToTarget(target, blob1);
            BlitBlobToTarget(target, blob2);
            BlitBlobToTarget(target, blob3);

            RenderTexture.active = active;
        }

        static void BlitBlobToTarget(RenderTexture target, RawImage blob)
        {
            if (blob == null || !blob.gameObject.activeInHierarchy)
                return;

            var rt = blob.rectTransform;
            var canvas = blob.canvas;
            if (canvas == null)
                return;

            var canvasRect = canvas.pixelRect;
            var corners = new Vector3[4];
            rt.GetWorldCorners(corners);

            var min = RectTransformUtility.WorldToScreenPoint(canvas.worldCamera ?? Camera.main, corners[0]);
            var max = RectTransformUtility.WorldToScreenPoint(canvas.worldCamera ?? Camera.main, corners[2]);

            var x = min.x / canvasRect.width;
            var y = min.y / canvasRect.height;
            var w = (max.x - min.x) / canvasRect.width;
            var h = (max.y - min.y) / canvasRect.height;
            BlitTexture(target, SoftCircleTexture, new Rect(x, y, w, h), blob.color);
        }

        static void BlitTexture(RenderTexture target, Texture source, Rect normalizedRect, Color tint)
        {
            if (source == null)
                return;

            var active = RenderTexture.active;
            RenderTexture.active = target;

            GL.PushMatrix();
            GL.LoadPixelMatrix(0, target.width, 0, target.height);

            var drawRect = new Rect(
                normalizedRect.x * target.width,
                normalizedRect.y * target.height,
                normalizedRect.width * target.width,
                normalizedRect.height * target.height);
            Graphics.DrawTexture(drawRect, source, new Rect(0, 0, 1, 1), 0, 0, 0, 0, tint);

            GL.PopMatrix();
            RenderTexture.active = active;
        }

        static Texture2D CreateGradientTexture()
        {
            const int height = 256;
            var texture = new Texture2D(1, height, TextureFormat.RGBA32, false)
            {
                wrapMode = TextureWrapMode.Clamp,
                filterMode = FilterMode.Bilinear,
                name = "GlassGradient"
            };

            var top = new Color(0.176f, 0.063f, 0.333f, 1f);    // #2D1055
            var bottom = new Color(0.420f, 0.184f, 0.659f, 1f); // #6B2FA8
            for (var y = 0; y < height; y++)
            {
                var t = y / (height - 1f);
                texture.SetPixel(0, y, Color.Lerp(bottom, top, t));
            }

            texture.Apply();
            return texture;
        }

        static Texture2D CreateSoftCircleTexture()
        {
            const int size = 128;
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false)
            {
                wrapMode = TextureWrapMode.Clamp,
                filterMode = FilterMode.Bilinear,
                name = "GlassSoftCircle"
            };

            var center = (size - 1) * 0.5f;
            var radius = size * 0.5f;
            for (var y = 0; y < size; y++)
            {
                for (var x = 0; x < size; x++)
                {
                    var dx = (x - center) / radius;
                    var dy = (y - center) / radius;
                    var dist = Mathf.Sqrt(dx * dx + dy * dy);
                    var alpha = Mathf.Clamp01(1f - dist);
                    alpha *= alpha;
                    texture.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
                }
            }

            texture.Apply();
            return texture;
        }
    }
}
