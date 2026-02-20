using UnityEngine;
using UnityEngine.Rendering;

#if UNITY_EDITOR
using UnityEditor;
#endif

public class Bridge : Singleton<Bridge>
{
    public GameObject[] blocks;
    public float blockSpacing = 2.0f;

    public float getBlockX(int index)
    {
        if (blocks == null || index < 0 || index >= blocks.Length)
            return 0f;
        
        return blocks[index].transform.position.x;
    }
}

#if UNITY_EDITOR
[CustomEditor(typeof(Bridge))]
public class BridgeEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        Bridge bridge = (Bridge)target;
        if (GUILayout.Button("Arrange Blocks"))
        {
            ArrangeBlocks(bridge);
        }
    }

    private void ArrangeBlocks(Bridge bridge)
    {
        if (bridge == null || bridge.blocks == null || bridge.blocks.Length == 0)
            return;

        float firstBlockX = bridge.transform.position.x - ((bridge.blocks.Length - 1) * bridge.blockSpacing) / 2;

        for (int i = 0; i < bridge.blocks.Length; i++)
        {
            GameObject block = bridge.blocks[i];
            if (block != null)
            {
                block.transform.position = bridge.transform.position + new Vector3(firstBlockX + i * bridge.blockSpacing, 0, 0);
            }
        }
    }
}
#endif