using PeopleOfMath.Data;
using UnityEngine;

namespace PeopleOfMath.UI
{
    public abstract class MathematicianDetailSection : MonoBehaviour
    {
        public abstract void Bind(MathematicianData data, bool english);

        public abstract string GetSectionTitle(bool english);
    }
}
