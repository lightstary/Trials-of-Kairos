using UnityEngine;
using UnityEngine.UI;

namespace TrialsOfKairos.UI
{
    /// <summary>
    /// Renders ambient drifting star particles on a UI canvas using a pool of small Image dots.
    /// Lightweight: no allocations after initialisation.
    /// </summary>
    public class StarfieldParticles : MonoBehaviour
    {
        [Header("Particle Settings")]
        [SerializeField] private int    particleCount  = 80;
        [SerializeField] private float  minSize        = 1f;
        [SerializeField] private float  maxSize        = 3.5f;
        [SerializeField] private float  minSpeed       = 4f;
        [SerializeField] private float  maxSpeed       = 18f;
        [SerializeField] private float  minAlpha       = 0.05f;
        [SerializeField] private float  maxAlpha       = 0.45f;
        [SerializeField] private Color  baseColor      = new Color(0.910f, 0.918f, 0.965f, 1f);
        [SerializeField] private Sprite dotSprite;

        private RectTransform[]  _rects;
        private float[]          _speeds;
        private Vector2          _canvasSize;
        private RectTransform    _parent;

        private void Awake()
        {
            _parent     = GetComponent<RectTransform>();
            _rects      = new RectTransform[particleCount];
            _speeds     = new float[particleCount];

            Rect r = _parent.rect;
            _canvasSize = new Vector2(r.width, r.height);

            for (int i = 0; i < particleCount; i++)
            {
                GameObject go = new GameObject($"Star_{i}", typeof(RectTransform), typeof(Image));
                go.transform.SetParent(_parent, false);

                RectTransform rt = go.GetComponent<RectTransform>();
                float s = Random.Range(minSize, maxSize);
                rt.sizeDelta        = new Vector2(s, s);
                rt.anchoredPosition = RandomPosition();
                _rects[i]           = rt;
                _speeds[i]          = Random.Range(minSpeed, maxSpeed);

                Image img   = go.GetComponent<Image>();
                img.sprite  = dotSprite;
                img.raycastTarget = false;
                Color c     = baseColor;
                c.a         = Random.Range(minAlpha, maxAlpha);
                img.color   = c;
            }
        }

        private void Update()
        {
            float dt = Time.unscaledDeltaTime;
            float hw = _canvasSize.x * 0.5f;
            float hh = _canvasSize.y * 0.5f;

            for (int i = 0; i < particleCount; i++)
            {
                Vector2 pos = _rects[i].anchoredPosition;
                pos.y -= _speeds[i] * dt;
                if (pos.y < -hh) pos = new Vector2(Random.Range(-hw, hw), hh);
                _rects[i].anchoredPosition = pos;
            }
        }

        private Vector2 RandomPosition()
        {
            float hw = _canvasSize.x * 0.5f;
            float hh = _canvasSize.y * 0.5f;
            return new Vector2(Random.Range(-hw, hw), Random.Range(-hh, hh));
        }
    }
}
