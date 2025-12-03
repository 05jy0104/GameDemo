using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class SpikeTrapAnimation : MonoBehaviour
{
    [Header("动画设置")]
    public List<Transform> spikeMeshes = new List<Transform>();
    public float hideSpeed = 3.0f;
    public float showSpeed = 2.0f;
    public float hideDistance = 0.3f;

    [Header("状态")]
    public bool startHidden = false;
    public bool isHidden = false;

    [Header("触发器设置")]
    public bool isTriggerTrap = true;
    public float triggerDelay = 0.5f;
    public float resetDelay = 2.0f;
    public bool oneTimeUse = false;

    [Header("伤害设置")]
    public bool causesDamage = true;
    public int damageAmount = 1;

    [Header("启动延迟")]
    public float startupDelay = 1.0f; // 新增：游戏启动延迟，避免一开始就触发

    private List<Vector3> shownPositions = new List<Vector3>();
    private List<Vector3> hiddenPositions = new List<Vector3>();
    private bool isTriggered = false;
    private bool playerInTrigger = false;
    private bool isReady = false; // 新增：标记陷阱是否就绪

    void Start()
    {
        // 如果没有指定刺，自动查找所有spike_开头的物体
        if (spikeMeshes.Count == 0)
        {
            FindAllSpikes();
        }

        if (spikeMeshes.Count > 0)
        {
            // 初始化所有刺的位置
            foreach (Transform spike in spikeMeshes)
            {
                if (spike != null)
                {
                    shownPositions.Add(spike.localPosition);
                    hiddenPositions.Add(spike.localPosition - new Vector3(0, hideDistance, 0));
                }
            }

            // 设置初始状态
            isHidden = startHidden;
            SetInitialPosition();

            // 设置标签和碰撞器
            SetupCollider();

            // 启动延迟，避免游戏一开始就触发
            StartCoroutine(StartupDelay());
        }
        else
        {
            Debug.LogError("没有找到任何刺的网格！");
        }
    }

    IEnumerator StartupDelay()
    {
        yield return new WaitForSeconds(startupDelay);
        isReady = true;
        Debug.Log($"陷阱已就绪: {gameObject.name}");
    }

    void Update()
    {
        if (spikeMeshes.Count == 0) return;

        // 为所有刺执行平滑动画
        for (int i = 0; i < spikeMeshes.Count; i++)
        {
            if (spikeMeshes[i] != null)
            {
                Vector3 targetPosition = isHidden ? hiddenPositions[i] : shownPositions[i];
                float currentSpeed = isHidden ? hideSpeed : showSpeed;

                spikeMeshes[i].localPosition = Vector3.Lerp(
                    spikeMeshes[i].localPosition,
                    targetPosition,
                    Time.deltaTime * currentSpeed
                );
            }
        }
    }

    // 自动查找所有刺
    private void FindAllSpikes()
    {
        foreach (Transform child in GetComponentsInChildren<Transform>())
        {
            if (child.name.StartsWith("spike_"))
            {
                spikeMeshes.Add(child);
                Debug.Log("找到刺: " + child.name);
            }
        }
    }

    // 设置碰撞器
    private void SetupCollider()
    {
        // 确保有碰撞器
        Collider collider = GetComponent<Collider>();
        if (collider == null)
        {
            // 添加盒形碰撞器
            BoxCollider boxCollider = gameObject.AddComponent<BoxCollider>();
            boxCollider.isTrigger = true;

            // 设置合适的大小
            Renderer renderer = GetComponent<Renderer>();
            if (renderer != null)
            {
                boxCollider.size = renderer.bounds.size;
                boxCollider.center = renderer.bounds.center - transform.position;
            }
            else
            {
                boxCollider.size = new Vector3(2, 1, 2);
                boxCollider.center = new Vector3(0, 0.5f, 0);
            }
        }
        else
        {
            collider.isTrigger = true;
        }

        // 设置标签
        gameObject.tag = "Trap";
    }

    // 设置初始位置
    private void SetInitialPosition()
    {
        for (int i = 0; i < spikeMeshes.Count; i++)
        {
            if (spikeMeshes[i] != null)
            {
                spikeMeshes[i].localPosition = isHidden ? hiddenPositions[i] : shownPositions[i];
            }
        }
    }

    void OnTriggerEnter(Collider other)
    {
        // 如果陷阱未就绪，不处理
        if (!isReady || !isTriggerTrap || isTriggered) return;

        if (other.CompareTag("Player"))
        {
            playerInTrigger = true;

            if (!oneTimeUse || !isTriggered)
            {
                StartCoroutine(TriggerTrap());
            }
        }
    }

    void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            playerInTrigger = false;
        }
    }

    IEnumerator TriggerTrap()
    {
        isTriggered = true;
        Debug.Log($"陷阱触发: {gameObject.name}");

        // 延迟后触发
        yield return new WaitForSeconds(triggerDelay);

        // 弹出刺
        ShowSpike();

        // 如果造成伤害且玩家仍在触发器中
        if (causesDamage && playerInTrigger)
        {
            // 对玩家造成伤害
            GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
            if (playerObj != null)
            {
                Debug.Log($"玩家被{gameObject.name}刺伤！");

                // 直接触发游戏结束
                ObstacleCollision obstacleCollision = playerObj.GetComponent<ObstacleCollision>();
                if (obstacleCollision != null)
                {
                    // 调用公共方法
                    obstacleCollision.SendMessage("TriggerGameOver", SendMessageOptions.DontRequireReceiver);
                }
            }
        }

        // 如果不是一次性陷阱，重置
        if (!oneTimeUse)
        {
            yield return new WaitForSeconds(resetDelay);
            HideSpike();
            isTriggered = false;
        }
    }

    public void ToggleSpike()
    {
        isHidden = !isHidden;
        Debug.Log("刺陷阱状态: " + (isHidden ? "隐藏" : "显示"));
    }

    public void HideSpike()
    {
        isHidden = true;
    }

    public void ShowSpike()
    {
        isHidden = false;
    }

    // 手动触发陷阱
    public void ActivateTrap()
    {
        if (!isTriggered && isReady)
        {
            StartCoroutine(TriggerTrap());
        }
    }
}