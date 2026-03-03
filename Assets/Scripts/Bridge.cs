using UnityEngine;
using UnityEngine.Rendering;
using System.Collections;

#if UNITY_EDITOR
using UnityEditor;
#endif

public class Bridge : Singleton<Bridge>
{
    public GameObject[] blocks;
    public float blockSpacing = 2.0f;
    [SerializeField] private float blinkSpeed = 2.0f;

    private int destroyedIdx = 0;
    private Coroutine blinkCoroutine;

    public float GetBlockX(int index)
    {
        if (blocks == null || index < 0 || index >= blocks.Length)
            return 0f;

        return blocks[index].transform.position.x;
    }

    public void BeforeDestroyBlock()
    {
        blinkCoroutine = StartCoroutine(BlinkBlock());
    }

    private IEnumerator BlinkBlock()
    {
        float elapsedTime = 0f;
        Renderer leftBlockRenderer = blocks[destroyedIdx + 1].GetComponent<Renderer>();
        Renderer rightBlockRenderer = blocks[blocks.Length - destroyedIdx - 2].GetComponent<Renderer>();

        Color blockColor = leftBlockRenderer.material.color;

        while (true)
        {
            float alpha = Mathf.PingPong(elapsedTime * blinkSpeed + 1f, 1f);
            blockColor.a = alpha;

            leftBlockRenderer.material.color = blockColor;
            rightBlockRenderer.material.color = blockColor;

            elapsedTime += Time.deltaTime;
            yield return null;
        }
    }

    public void DestroyBlock()
    {
        if (blinkCoroutine != null)
        {
            StopCoroutine(blinkCoroutine);
        }
        destroyedIdx++;
        blocks[destroyedIdx].SetActive(false);
        blocks[blocks.Length - destroyedIdx - 1].SetActive(false);
    }

    public bool IsOutOfRange(int pos)
    {
        return pos <= destroyedIdx || pos >= blocks.Length - 1 - destroyedIdx;
    }
}


/*
    Bridge의 블록을 Editor에서 자동으로 배치하는 스크립트입니다.
    Bridge 오브젝트에 있는 blocks 배열에 블록들을 할당하고 block spacing을 설정하면, "Arrange Blocks" 버튼을 누르면 블록들이 균등하게 배치됩니다.
*/
#if UNITY_EDITOR
[CustomEditor(typeof(Bridge))]
public class BridgeEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        Bridge bridge = (Bridge) target;
        if (GUILayout.Button("Arrange Blocks"))
        {
            ArrangeBlocks(bridge);
        }
    }

    private void ArrangeBlocks(Bridge bridge)
    {
        if (bridge == null || bridge.blocks == null || bridge.blocks.Length == 0)
        {
            return;
        }

        float firstBlockX = bridge.transform.position.x - (bridge.blocks.Length - 1) * bridge.blockSpacing / 2;

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