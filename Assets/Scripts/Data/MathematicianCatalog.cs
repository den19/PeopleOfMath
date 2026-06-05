using System.Collections.Generic;
using UnityEngine;

namespace PeopleOfMath.Data
{
    [CreateAssetMenu(fileName = "MathematicianCatalog", menuName = "PeopleOfMath/Mathematician Catalog")]
    public class MathematicianCatalog : ScriptableObject
    {
        public List<MathematicianData> mathematicians = new();

        public IReadOnlyList<MathematicianData> All => mathematicians;
    }
}
