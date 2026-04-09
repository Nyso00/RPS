using UnityEngine;
using System.Collections;

#if UNITY_EDITOR
using UnityEditor;
#endif

public class Bridge : Singleton<Bridge>
{
    public GameObject BlockPrefab;

    [Tooltip("다리의 한쪽 방향에 있을 블록 개수\n(= 게임 승리에 필요한 승점)")]
    [SerializeField] private int _blockCountOfOneSide = 5;
    [SerializeField] private float _blockSpacing = 1.1f;
    [SerializeField] private float _blinkSpeed = 2.0f;
    [SerializeField] private GameObject[] _blocks;

    // Getters
    public int BlockCountOfOneSide => _blockCountOfOneSide;
    public float BlockSpacing => _blockSpacing;
    public GameObject[] Blocks => _blocks;

    private int _destroyedIdx = 0;
    private Coroutine _blinkCoroutine;

    /// <summary>
    /// Bridge에서 현재 상황에 따라 이동해야할 대상 블록의 X 좌표를 반환하는 함수입니다.
    /// </summary>
    /// <param name="score">현재 게임 스코어</param>
    /// <param name="leftSide">왼쪽에 위치한 플레이어인지 여부</param>
    /// <returns></returns>
    public float GetBlockX(int score, bool leftSide)
    {
        int index = BlockCountOfOneSide + (leftSide ? 0 : 1) + score;

        if (_blocks == null || index < 0 || index >= _blocks.Length)
        {
            return 0f;
        }

        return _blocks[index].transform.position.x;
    }

    /// <summary>
    /// 블록이 파괴되기 전에 양쪽 끝 블록이 깜빡이는 효과를 시작하는 함수입니다.
    /// </summary>
    public void BeforeDestroyBlock()
    {
        _blinkCoroutine = StartCoroutine(BlinkBlock());
    }

    private IEnumerator BlinkBlock()
    {
        float elapsedTime = 0f;
        Renderer leftBlockRenderer = _blocks[_destroyedIdx + 1].GetComponent<Renderer>();
        Renderer rightBlockRenderer = _blocks[_blocks.Length - _destroyedIdx - 2].GetComponent<Renderer>();

        Color blockColor = leftBlockRenderer.material.color;

        while (true)
        {
            float alpha = Mathf.PingPong(elapsedTime * _blinkSpeed + 1f, 1f);
            blockColor.a = alpha;

            leftBlockRenderer.material.color = blockColor;
            rightBlockRenderer.material.color = blockColor;

            elapsedTime += Time.deltaTime;
            yield return null;
        }
    }

    /// <summary>
    /// 양쪽 끝 블록을 파괴하는 함수입니다. 블록이 파괴되기 전에 BeforeDestroyBlock() 함수를 호출하여 깜빡이는 효과를 시작해야 합니다.
    /// </summary>
    public void DestroyBlock()
    {
        if (_blinkCoroutine != null)
        {
            StopCoroutine(_blinkCoroutine);
        }
        _destroyedIdx++;
        _blocks[_destroyedIdx].SetActive(false);
        _blocks[_blocks.Length - _destroyedIdx - 1].SetActive(false);
    }

#if UNITY_EDITOR
    public void EditorSetBlocks(GameObject[] newBlocks)
    {
        _blocks = newBlocks;
    }
#endif
}


// ----------------------------------------------------------------------------------------
//  Bridge의 블록을 Editor에서 자동으로 배치하는 스크립트입니다.
//  Bridge 오브젝트에 있는 Blocks 배열에 블록들을 할당하고 block spacing을 설정하면, "Arrange Blocks" 버튼을 누르면 블록들이 균등하게 배치됩니다.
// ----------------------------------------------------------------------------------------
#if UNITY_EDITOR
[CustomEditor(typeof(Bridge))]
public class BridgeEditor : Editor
{
    public override void OnInspectorGUI()
    {
        // 1. 기본 인스펙터 변수들(BlockPrefab, blockCount 등) 표시
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
        if (bridge.BlockPrefab == null)
        {
            Debug.LogWarning("Block Prefab이 할당되지 않았습니다! 인스펙터에서 프리팹을 넣어주세요.");
            return;
        }

        for (int i = bridge.transform.childCount - 1; i >= 0; i--)
        {
            GameObject child = bridge.transform.GetChild(i).gameObject;
            Undo.DestroyObjectImmediate(child);
        }

        int blockCount = bridge.BlockCountOfOneSide * 2 + 2; // 양쪽 블록 개수 + 보이지 않는 양 끝 블록

        GameObject[] tempBlocks = new GameObject[blockCount];

        for (int i = 0; i < blockCount; i++)
        {
            GameObject newBlock = (GameObject) PrefabUtility.InstantiatePrefab(bridge.BlockPrefab);

            // Undo 등록 (Ctrl + Z 로 생성 취소 가능하게 만듦)
            Undo.RegisterCreatedObjectUndo(newBlock, "Generate Bridge Blocks");

            newBlock.transform.SetParent(bridge.transform);
            newBlock.name = $"Block_{i}";

            tempBlocks[i] = newBlock;
        }

        bridge.EditorSetBlocks(tempBlocks);
        ArrangeBlocks(bridge);

        // 변경된 배열 데이터를 씬(Scene)에 확실히 저장하라고 유니티에 알림
        EditorUtility.SetDirty(bridge);
    }

    private void ArrangeBlocks(Bridge bridge)
    {
        if (bridge == null || bridge.Blocks == null || bridge.Blocks.Length == 0)
        {
            return;
        }

        float firstBlockX = bridge.transform.position.x - (bridge.Blocks.Length - 1) * bridge.BlockSpacing / 2;

        for (int i = 0; i < bridge.Blocks.Length; i++)
        {
            GameObject block = bridge.Blocks[i];
            if (block != null)
            {
                // 위치 변경 전 Undo 기록 (Ctrl+Z 지원)
                Undo.RecordObject(block.transform, "Arrange Bridge Blocks");
                block.transform.position = bridge.transform.position + new Vector3(firstBlockX + i * bridge.BlockSpacing, 0, 0);
            }
        }

        bridge.Blocks[0].SetActive(false);
        bridge.Blocks[^1].SetActive(false);
    }
}
#endif