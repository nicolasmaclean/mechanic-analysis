using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Game.Puzzle
{
    public class PlayerPath : MonoBehaviour
    {
        #region Public Variables
        [HideInInspector]
        public List<Vector2> Verts = new List<Vector2>();
        #endregion

        #region Exposed Variables
        [Header("Visual Configurations")]
        [Tooltip("Material to be applied to created visuals.")]
        [SerializeField]
        Material _lineMaterial;

        [Tooltip("The animation duration of path start")]
        [SerializeField, Min(0)]
        float _startAnimationTime = .15f;

        [Tooltip("The animation duration of path clearing")]
        [SerializeField, Min(0)]
        float _stopAnimationTime = .2f;

        [Tooltip("The final color of the path before destroying itself. Should be the same as the puzzle path.")]
        [SerializeField]
        Color _finalColor;

        [Tooltip("The cycle length of the pulsing animation performed when the player has reached the end of the puzzle.")]
        [SerializeField]
        float _winCycle = .8f;

        [Tooltip("Wire gameobject to have its material change upon completing the puzzle.")]
        [SerializeField]
        GameObject _GOWire;
        #endregion

        #region Private Variables
        PuzzleRenderer _puzzle;
        Game.Player.Player _player;
        List<GameObject> _visuals = new List<GameObject>();
        bool _clearing = false;

        Material _instancedMaterial;
        int _emissionID;
        Color _initialColor;
        Color _initialEmission;

        Renderer _wireRenderer;
        Color _initialWireEmission;

        float _elapsedTime = 0;
        #endregion

        #region Monobehaviour
        void Awake()
        {
            _puzzle = transform.parent.GetComponent<PuzzleRenderer>();
            if (_puzzle == null)
            {
                Debug.LogError("ERROR: unable to find parent PuzzleRenderer.");
            }

            _instancedMaterial = new Material(_lineMaterial.shader);
            _instancedMaterial.CopyPropertiesFromMaterial(_lineMaterial);
            _emissionID = Shader.PropertyToID("_EmissionColor");

            _initialColor = _instancedMaterial.color;
            _initialEmission = _instancedMaterial.GetColor(_emissionID);

            _wireRenderer = _GOWire.GetComponent<Renderer>();
            _initialWireEmission = _wireRenderer.materials[1].GetColor(_emissionID);
            Color b = Color.black;
            b.a = _initialWireEmission.a;
            _wireRenderer.materials[1].SetColor(_emissionID, b);
        }

        void Update()
        {
            if (_player != null && _player.AtEnd && !_player.Won && !_clearing)
            {
                PulsateEmission();
                _elapsedTime += Time.deltaTime;
            }
            else
            {
                _instancedMaterial.SetColor(_emissionID, _initialEmission);
                _elapsedTime = 0;
            }
        }
        #endregion

        #region Drawing
        /// <summary>
        ///     Starts drawing PlayerPath at the given coordinate.
        ///     Will not do anything if a current playerPath is still clearing out.
        /// </summary>
        /// <param name="puzzle"></param>
        /// <param name="pos"></param>
        public bool StartPath(Vector2 position, Game.Player.Player player)
        {
            if (_clearing) return false;

            ClearPath();
            _player = player;

            _instancedMaterial.color = _initialColor;
            _instancedMaterial.SetColor(_emissionID, _initialEmission);

            Vector3 localPosition = _puzzle.PuzzleToLocal(position);
            GameObject go = MeshLine.DrawStartPoint(_puzzle.configs, transform, localPosition);
            AddVisual(go);

            StartCoroutine(ScaleFromZeroTo(go.transform, Vector3.one * _puzzle.configs.lineWidth * _puzzle.configs.startNodeSize, _startAnimationTime));
            return true;
        }

        /// <summary>
        ///     Begins destroying the path. _clearing is true while doing so.
        /// </summary>
        public void StopPath()
        {
            _clearing = true;

            StartCoroutine(FadeAway(_instancedMaterial, _finalColor, _stopAnimationTime));
            StartCoroutine(WaitBeforeCallback(_stopAnimationTime, ClearPath));

            StartCoroutine(WaitBeforeCallback(_stopAnimationTime, () =>
            {
                _clearing = false;
            }));
        }

        /// <summary>
        ///     Adds vert if appropriate. Assumes input does not include self-intersecting lines.
        /// </summary>
        /// <param name="position"> Desired marker position in puzzle space </param>
        /// <param name="target"> The target puzzle position </param>
        public void SetMarker(Vector2 position, Vector2 target)
        {
            if (!Verts.Contains(position) && (Verts.Count == 0 || Verts.Count > 0 && Verts[Verts.Count - 1] != target))
            {
                Verts.Add(position);
            }
            // case: retracing
            else if (Verts.Count > 1 && Verts[Verts.Count - 1] == position && Verts[Verts.Count - 2] == target)
            {
                RemoveMarker();
                return;
            }
            // case: 
            else if (!Verts.Contains(position) && Verts.Count > 0 && Verts[Verts.Count - 1] == target)
            {
                return;
            }
            // case: switch directions from previous intersection
            else
            {
                RemoveLastLine();
            }

            // snaps previous line segment to end point and starts next line segment
            UpdatePath(position);
            CreateLine(position, target);
        }

        /// <summary>
        ///     Updates the last line segment towards the given position.
        ///     Project position onto the segments direction vector.
        /// </summary>
        /// <param name="position"></param>
        public void UpdatePath(Vector2 position)
        {
            if (_visuals.Count < 2 || _clearing) return;

            Transform lineT = _visuals[_visuals.Count - 2].transform;
            Transform capT = _visuals[_visuals.Count - 1].transform;

            Vector3 localPosition = _puzzle.PuzzleToLocal(position);
            Vector3 localStart = _puzzle.transform.InverseTransformPoint(lineT.position);
            Vector3 localEnd = Vector3.Project(localPosition - localStart, lineT.up) + localStart;

            Vector3 nScal = lineT.localScale;
            nScal.y = Vector3.Distance(localStart, localEnd);
            lineT.localScale = nScal;
            capT.localPosition = localEnd;
        }

        public void Complete()
        {
            StartCoroutine(FadeEmission(_wireRenderer.materials[1], _initialWireEmission, .8f));
        }

        /// <summary>
        ///     Pops the last marker
        /// </summary>
        void RemoveMarker()
        {
            if (Verts.Count == 0) return;
            Verts.RemoveAt(Verts.Count - 1);

            RemoveLastLine();
        }
        #endregion

        #region Utility
        /// <summary>
        ///     Destroys visuals composing this path.
        /// </summary>
        void ClearPath()
        {
            foreach (GameObject go in _visuals)
            {
                Destroy(go);
            }
            _visuals.Clear();
            Verts.Clear();
        }

        /// <summary>
        ///     Removes the last 2 gameObjects in _visuals, if able to.
        /// </summary>
        void RemoveLastLine()
        {
            if (_visuals.Count > 1)
            {
                int lastIndex = _visuals.Count - 1;
                Destroy(_visuals[lastIndex]);
                Destroy(_visuals[lastIndex - 1]);

                _visuals.RemoveRange(lastIndex - 1, 2);
            }
        }

        /// <summary>
        ///     Creates a line at position towards the target. Defaults length to _puzzle.configs.linewidth / 2
        /// </summary>
        /// <param name="position"> </param>
        /// <param name="target"> </param>
        void CreateLine(Vector2 position, Vector2 target)
        {
            Vector3 localPosition = _puzzle.PuzzleToLocal(position);
            Vector3 localEnd = localPosition + (Vector3)(target - position) * _puzzle.configs.lineWidth / 2;
            GameObject[] lineVisuals = MeshLine.DrawLineRounded(_puzzle.configs, transform, localPosition, localEnd, false, true);

            foreach (GameObject vis in lineVisuals)
            {
                AddVisual(vis);
            }
        }

        /// <summary>
        ///     Configures prefab instance. Assumes the visual is a child of this object.
        ///     Ignores null GameObjects.
        /// </summary>
        /// <param name="go"></param>
        void AddVisual(GameObject go)
        {
            if (go == null) return;
            _visuals.Add(go);

            Collider collider = go.GetComponent<Collider>();
            if (collider != null)
            {
                collider.enabled = false;
            }

            go.transform.GetComponentInChildren<Renderer>().material = _instancedMaterial;
        }
        #endregion

        #region Coroutines and VFX
        IEnumerator ScaleFromZeroTo(Transform t, Vector3 finalScale, float duration)
        {
            float timestamp = Time.time;

            while (Time.time - timestamp < duration)
            {
                t.localScale = Vector3.Lerp(Vector3.zero, finalScale, (Time.time - timestamp) / duration);
                yield return null;
            }

            t.localScale = finalScale;
        }

        IEnumerator FadeAway(Material mat, Color fColor, float duration)
        {
            float timestamp = Time.time;
            float transition = .7f;
            Color eColor = mat.GetColor(_emissionID);

            while (Time.time - timestamp < duration)
            {
                float elapsedTime = Time.time - timestamp;

                mat.color = Color.Lerp(_initialColor, fColor, (elapsedTime - transition) / (duration - transition));
                mat.SetColor(_emissionID, Color.Lerp(eColor, Color.black, elapsedTime / transition));

                yield return null;
            }

            mat.color = fColor;
            mat.SetColor(_emissionID, Color.black);
        }

        void PulsateEmission()
        {
            float t = .5f * Mathf.Sin(2 * Mathf.PI * _elapsedTime / _winCycle) + .5f;
            Color nColor = Color.Lerp(Color.white, _initialEmission, t);
            _instancedMaterial.SetColor(_emissionID, nColor);
        }

        IEnumerator WaitBeforeCallback(float waitTime, System.Action callback)
        {
            yield return new WaitForSeconds(waitTime);
            callback();
        }

        IEnumerator FadeEmission(Material mat, Color fColor, float duration)
        {
            Color iColor = mat.GetColor(_emissionID);
            fColor.a = iColor.a;

            if (fColor == iColor) { yield break; }
            float elapsedtime = 0;

            while (elapsedtime < duration)
            {
                mat.SetColor(_emissionID, Color.Lerp(iColor, fColor, elapsedtime / duration));

                yield return null;
                elapsedtime += Time.deltaTime;
            }

            mat.SetColor(_emissionID, fColor);
        }
        #endregion
    }
}