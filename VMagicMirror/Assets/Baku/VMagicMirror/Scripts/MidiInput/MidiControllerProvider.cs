using UnityEngine;

namespace Baku.VMagicMirror
{
    public class MidiControllerProvider : MonoBehaviour
    {
        //NOTE: 中指の位置ベースだと隣り合うキーを叩くときに左右の手が重なりすぎるので、それを防ぐやつ
        private const float NoteFingerOffset = 0.03f;
        private const float KnobFingerOffset = 0.015f;
        
        [SerializeField] private Transform notePrefab = null;
        [SerializeField] private int noteRow = 8;
        [SerializeField] private int noteCol = 8;
        [SerializeField] private float noteInterval = 0.035f;

        //NOTE: knobは[0,1]のvalueを指定して値が切り替わるべきでは
        [SerializeField] private MidiKnob knobPrefab = null;
        [SerializeField] private int knobCount = 8;
        [Tooltip("ノブ中心の、ノート中心に対するオフセット")]
        [SerializeField] private Vector3 knobOffset = new Vector3(0, 0.1f, 0.2f);
        [SerializeField] private float knobInterval = 0.04f;

        [SerializeField] private Vector3 initPos = new Vector3(0, 1.0f, 0.3f);
        [SerializeField] private Vector3 initRot = new Vector3(0, 0, 0);
        [SerializeField] private Vector3 initScale = new Vector3(0.7f, 0.7f, 0.7f);
        
        
        private Transform[] _notes = null;
        private MidiKnob[] _knobs = null;
        public MidiKnob[] Knobs => _knobs ?? new MidiKnob[0];

        public int LeftKnobCenterIndex => Mathf.Clamp(knobCount / 2 - 1, 0, knobCount - 1);
        public int RightKnobCenterIndex => knobCount / 2;        

        private void Awake()
        {
            InitializeLayout();
            CombineMeshes();
            transform.position = initPos;
            transform.rotation = Quaternion.Euler(initRot);
            transform.localScale = initScale;
        }

        private void InitializeLayout()
        {
            var t = transform;
            
            //ノートの生成
            _notes = new Transform[noteRow * noteCol];
            float halfDepth = 0.5f * noteInterval * (noteRow - 1);
            float halfWidth = 0.5f * noteInterval * (noteCol - 1);
            for (int i = 0; i < noteRow; i++)
            {
                for (int j = 0; j < noteCol; j++)
                {
                    var note = Instantiate(notePrefab, t);
                    _notes[i * noteCol + j] = note;
                    note.localPosition = new Vector3(
                        j * noteInterval - halfWidth,
                        0,
                        i * noteInterval - halfDepth
                    );
                    note.GetComponentInChildren<MeshRenderer>().material = HIDMaterialUtil.Instance.GetMidiNoteMaterial();
                }
            }
            
            //ノブの生成
            _knobs = new MidiKnob[knobCount];
            float knobHalfWidth = 0.5f * knobInterval * (knobCount - 1);
            for (int i = 0; i < knobCount; i++)
            {
                var knob = Instantiate(knobPrefab, t);
                _knobs[i] = knob;
                knob.transform.localPosition =
                    knobOffset +
                    Vector3.right * (i * knobInterval - knobHalfWidth);
            }
        }

        /// <summary>
        /// 指定したノートに対応する位置情報を取得します。
        /// </summary>
        /// <param name="noteNumber"></param>
        /// <returns></returns>
        public MidiNoteTargetData GetNoteTargetData(int noteNumber)
        {
            noteNumber = noteNumber % (noteRow * noteCol);
            int targetRow = noteNumber / noteCol;
            int targetCol = noteNumber % noteCol;

            //左側は左中指、右側は右中指にしちゃう。わかりやすいので
            int targetFinger =
                (targetCol < noteCol / 2) ? FingerConsts.LeftMiddle : FingerConsts.RightMiddle;

            var t = _notes[noteNumber];
            var pos = t.position;
            var posWithOffset = 
                pos +
                t.right * ((targetFinger == FingerConsts.LeftMiddle ? -1 : 1) * NoteFingerOffset);

            return new MidiNoteTargetData()
            {
                row = targetRow,
                col = targetCol,
                fingerNumber = targetFinger,
                noteTransform = t,
                position = pos,
                positionWithOffset = posWithOffset,
            };
        }

        /// <summary>
        /// ノブとノブの値を指定して、位置情報を取得します。
        /// </summary>
        /// <param name="knobNumber"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        public MidiKnobTargetData GetKnobTargetData(int knobNumber, float value)
        {
            knobNumber = knobNumber % knobCount;
            _knobs[knobNumber].SetValue(value);
            var result = _knobs[knobNumber].GetKnobTargetData();

            //左半分のノブは左手で動かす、という意味
            result.isLeftHandPreferred = (knobNumber < knobCount / 2);
            result.position +=
                ((result.isLeftHandPreferred ? -1 : 1) * KnobFingerOffset) *
                result.knobTransform.right;
            return result;
        }

        //NOTE: ノートのメッシュはくっつけてOK。ノブについては値ベースで動かすので分けたままにしとく
        private void CombineMeshes()
        {
            var meshFilters = new MeshFilter[_notes.Length];
            for (int i = 0; i < _notes.Length; i++)
            {
                meshFilters[i] = _notes[i].GetComponentInChildren<MeshFilter>();
            }
            CombineInstance[] combine = new CombineInstance[meshFilters.Length];

            for (int i = 0; i < meshFilters.Length; i++)
            {
                combine[i].mesh = meshFilters[i].sharedMesh;
                combine[i].transform = meshFilters[i].transform.localToWorldMatrix;
                meshFilters[i].gameObject.SetActive(false);
            }

            var meshFilter = GetComponent<MeshFilter>();
            meshFilter.mesh = new Mesh();
            meshFilter.mesh.CombineMeshes(combine);
        }
        
    }
    
    public struct MidiNoteTargetData
    {
        public int row;
        public int col;
        public int fingerNumber;
        public Transform noteTransform;
        public Vector3 position;
        public Vector3 positionWithOffset;

        public bool IsLeftHandPreffered => fingerNumber < 5;
    }

    public struct MidiKnobTargetData
    {
        public Transform knobTransform;
        public Vector3 position;
        public bool isLeftHandPreferred;
    }

}
