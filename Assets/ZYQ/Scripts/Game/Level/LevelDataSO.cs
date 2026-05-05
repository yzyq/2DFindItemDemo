using System.Collections.Generic;
using UnityEngine;

namespace ZYQ.Demo
{
    [CreateAssetMenu(fileName = "LevelDataSO", menuName = "ZYQ/Level Database")]
    public class LevelDataSO : ScriptableObject
    {
        [SerializeField] private List<LevelData> levels = new List<LevelData>();

        public IReadOnlyList<LevelData> Levels => levels;

        public LevelData GetLevel(int levelId)
        {
            for (int i = 0; i < levels.Count; i++)
            {
                if (levels[i] != null && levels[i].LevelId == levelId)
                {
                    return levels[i];
                }
            }

            return null;
        }
    }
}