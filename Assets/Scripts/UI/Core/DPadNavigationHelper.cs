using UnityEngine;
using UnityEngine.EventSystems;

namespace TrialsOfKairos.UI
{
    /// <summary>
    /// Injects D-pad navigation into the EventSystem.
    ///
    /// Xbox controllers map D-pad to joystick axes 6 (horizontal) and 7 (vertical,
    /// inverted). Unity's StandaloneInputModule only reads "Horizontal"/"Vertical"
    /// axes (left stick), so D-pad navigation must be injected manually.
    ///
    /// This component reads two InputManager axes: "DPadHorizontal" and "DPadVertical".
    /// If those axes are missing, run: Tools > Add D-Pad Input Axes.
    /// </summary>
    [RequireComponent(typeof(EventSystem))]
    public class DPadNavigationHelper : MonoBehaviour
    {
        private const float DEAD_ZONE     = 0.5f;
        private const float INITIAL_DELAY = 0.40f;
        private const float REPEAT_DELAY  = 0.20f;

        private const string AXIS_H = "DPadHorizontal";
        private const string AXIS_V = "DPadVertical";

        private EventSystem _eventSystem;
        private bool        _axesExist;

        private bool  _heldH;
        private bool  _heldV;
        private float _nextH;
        private float _nextV;

        private void Awake()
        {
            _eventSystem = GetComponent<EventSystem>();
            _axesExist   = AxisExists(AXIS_H) && AxisExists(AXIS_V);

            if (!_axesExist)
                Debug.LogWarning("[DPadNavigationHelper] D-pad axes not found. " +
                                 "Run Tools > Add D-Pad Input Axes to configure them.");
        }

        private void Update()
        {
            if (!_axesExist) return;
            if (_eventSystem == null || _eventSystem.currentSelectedGameObject == null) return;

            float h = Input.GetAxisRaw(AXIS_H);
            float v = Input.GetAxisRaw(AXIS_V);

            ProcessAxis(h, ref _heldH, ref _nextH, MoveDirection.Left,  MoveDirection.Right);
            ProcessAxis(v, ref _heldV, ref _nextV, MoveDirection.Down, MoveDirection.Up);
        }

        private void ProcessAxis(float raw, ref bool held, ref float nextTime,
                                 MoveDirection negDir, MoveDirection posDir)
        {
            if (Mathf.Abs(raw) > DEAD_ZONE)
            {
                MoveDirection dir = raw < 0f ? negDir : posDir;

                if (!held)
                {
                    SendMove(dir);
                    held     = true;
                    nextTime = Time.unscaledTime + INITIAL_DELAY;
                }
                else if (Time.unscaledTime >= nextTime)
                {
                    SendMove(dir);
                    nextTime = Time.unscaledTime + REPEAT_DELAY;
                }
            }
            else
            {
                held = false;
            }
        }

        private void SendMove(MoveDirection dir)
        {
            if (_eventSystem.currentSelectedGameObject == null) return;

            AxisEventData data = new AxisEventData(_eventSystem) { moveDir = dir };
            ExecuteEvents.Execute(
                _eventSystem.currentSelectedGameObject,
                data,
                ExecuteEvents.moveHandler);
        }

        private static bool AxisExists(string name)
        {
            try   { Input.GetAxisRaw(name); return true; }
            catch { return false; }
        }
    }
}
