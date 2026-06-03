using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace PeopleOfMath.Data
{
    public class MathematicianRepository : MonoBehaviour
    {
        [SerializeField] List<MathematicianData> mathematicians = new();

        readonly Dictionary<string, MathematicianData> _byId = new();

        public IReadOnlyList<MathematicianData> All => mathematicians;

        void Awake()
        {
            _byId.Clear();
            foreach (var m in mathematicians)
            {
                if (m != null && !string.IsNullOrEmpty(m.id))
                    _byId[m.id] = m;
            }
        }

        public MathematicianData GetById(string id)
        {
            return id != null && _byId.TryGetValue(id, out var data) ? data : null;
        }

        public void SetMathematicians(List<MathematicianData> list)
        {
            mathematicians = list.Where(m => m != null).ToList();
            Awake();
        }
    }
}
