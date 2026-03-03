using UnityEngine;
using UnityEngine.Rendering;
using System.Collections;

#if UNITY_EDITOR
using UnityEditor;
#endif

public class Bridge : Singleton<Bridge>
{
    public GameObject blockPrefab;

    [Tooltip("다리의 한쪽 방향에 있을 블록 개수\n(= 게임 승리에 필요한 승점)")]
    public int blockCountOfOneSide = 5;
    public float blockSpacing = 2.0f;
    [SerializeField] private float blinkSpeed = 2.0f;

    [HideInInspector] public GameObject[] blocks;

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
        // 1. 기본 인스펙터 변수들(blockPrefab, blockCount 등) 표시
        DrawDefaultInspector();

        Bridge bridge = (Bridge) target;

        GUILayout.Space(10); // UI 위아래 간격 살짝 띄우기

        // 2. 새로운 버튼: 설정한 개수만큼 프리팹을 생성하고 배치
        if (GUILayout.Button("Generate & Arrange Blocks"))
        {
            GenerateBlocks(bridge);
        }

        // 3. 기존 버튼: 이미 배열에 있는 블록들의 간격만 재조정
        if (GUILayout.Button("Arrange Existing Blocks"))
        {
            ArrangeBlocks(bridge);
        }
    }

    private void GenerateBlocks(Bridge bridge)
    {
        if (bridge.blockPrefab == null)
        {
            Debug.LogWarning("Block Prefab이 할당되지 않았습니다! 인스펙터에서 프리팹을 넣어주세요.");
            return;
        }

        if (bridge.blocks != null)
        {
            for (int i = 0; i < bridge.blocks.Length; i++)
            {
                if (bridge.blocks[i] != null)
                {
                    Undo.DestroyObjectImmediate(bridge.blocks[i]);
                }
            }
        }

        int blockCount = bridge.blockCountOfOneSide * 2 + 2; // 양쪽 블록 개수 + 보이지 않는 양 끝 블록

        bridge.blocks = new GameObject[blockCount];

        for (int i = 0; i < blockCount; i++)
        {
            GameObject newBlock = (GameObject) PrefabUtility.InstantiatePrefab(bridge.blockPrefab);

            // Undo 등록 (Ctrl + Z 로 생성 취소 가능하게 만듦)
            Undo.RegisterCreatedObjectUndo(newBlock, "Generate Bridge Blocks");

            newBlock.transform.SetParent(bridge.transform);
            newBlock.name = $"Block_{i}";

            bridge.blocks[i] = newBlock;
        }

        ArrangeBlocks(bridge);

        // 변경된 배열 데이터를 씬(Scene)에 확실히 저장하라고 유니티에 알림
        EditorUtility.SetDirty(bridge);
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
                // 위치 변경 전 Undo 기록 (Ctrl+Z 지원)
                Undo.RecordObject(block.transform, "Arrange Bridge Blocks");
                block.transform.position = bridge.transform.position + new Vector3(firstBlockX + i * bridge.blockSpacing, 0, 0);
            }
        }

        bridge.blocks[0].SetActive(false);
        bridge.blocks[bridge.blocks.Length - 1].SetActive(false);
    }
}
#endif