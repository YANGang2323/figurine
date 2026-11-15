using System.Runtime.InteropServices;
using UnityEngine;

// 카메라 시점을 회전시키는 스크립트 (싱글톤 적용)
// Q는 측면 시점, E는 정면 시점으로 전환
public class CameraPivotRotator : MonoBehaviour
{
    // 싱글톤 인스턴스
    public static CameraPivotRotator Instance { get; private set; }
    public event System.Action ViewTurnStarted;

    [Header("Rotation Settings")]
    public float rotationSpeed = 5f;          // 회전 속도 조절
    public bool isSideView;           // 현재 측면 시점 여부 (true면 시점2)
    private bool isRotating = false;
    private Vector3 turnAnchor;
    private Quaternion turnRotation;
    [SerializeField, Min(1f)] private float mapCameraDistance = 36f;
    private Transform mapRoot;
    private Vector3 mapTurnStartPosition;
    private bool mapTurnStartSide;
    private Vector3 mapTurnPivot;
    private Vector3 mapTurnOrigin;
    private Vector3 mapTurnSource;
    private float mapTurnProgress;
    [SerializeField] private BoxCollider[] cameraBoundaryWalls;
    [SerializeField, Min(0f)] private float cameraEdgeInset = 0.05f;
    private Vector3 mapTurnCameraOrigin;
    private Vector3 mapCameraPosition;
    private Vector3 mapFollowPlayerPosition;
    private Quaternion mapTurnStart;
    private Quaternion mapTurnTarget;
    private bool mapTurnBodyWasKinematic;
    private Rigidbody mapTurnBody;

    public bool UsesMapRotation => mapRoot != null;

    internal SavedCameraView ViewOnLoad { get; set; }

    // While the map is turning, keep the last settled position and view together.
    internal bool SaveSideView => mapRoot != null && isRotating ? mapTurnStartSide : isSideView;

    public Vector3 ToSavePosition(Vector3 worldPosition)
    {
        if (mapRoot == null)
            return worldPosition;

        return isRotating ? mapTurnSource : mapRoot.InverseTransformPoint(worldPosition);
    }

    private void InitializeMapRotation()
    {
        if (player == null || stage3CameraMode ||
            (gameObject.scene.name != "Stage1" && gameObject.scene.name != "Stage2"))
            return;

        viewCamera = GetComponentInChildren<Camera>();
        if (viewCamera == null)
            return;

        GameObject[] roots = gameObject.scene.GetRootGameObjects();
        bool stageTwo = gameObject.scene.name == "Stage2";
        bool distantMap = stageTwo && player.position.x > 150f;
        string rootName = stageTwo ? (distantMap ? "MapRoot First" : "MapRoot Second") : "MapRoot";
        var root = new GameObject(rootName);
        UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(root, gameObject.scene);
        mapRoot = root.transform;

        foreach (GameObject item in roots)
        {
            if (item.GetComponentInChildren<PlayerMovement>(true) != null ||
                item.GetComponentInChildren<Camera>(true) != null ||
                item.GetComponentInChildren<Canvas>(true) != null)
                continue;

            if (!stageTwo)
                item.transform.SetParent(mapRoot, true);
            else
                AttachStageTwoGeometry(item.transform, distantMap);
        }

        if (distantMap)
        {
            foreach (GameObject item in roots)
            {
                Stage2IntroTrigger entrance = item.GetComponentInChildren<Stage2IntroTrigger>(true);
                if (entrance != null)
                    entrance.transform.SetParent(mapRoot, true);
            }
        }

        transform.rotation = defaultRotation;
        Vector3 lensOffset = viewCamera.transform.localPosition;
        lensOffset.z = -mapCameraDistance;
        viewCamera.transform.localPosition = lensOffset;
        mapCameraPosition = GetFollowPosition(defaultRotation);
        transform.position = mapCameraPosition;
        ApplyMapView(isSideView);
        mapFollowPlayerPosition = player.position;
    }

    private void AttachStageTwoGeometry(Transform item, bool distantMap)
    {
        // The shared wall container holds boundaries from both Stage2 areas.
        if (item.name == "Unvisible Wall")
        {
            for (int index = item.childCount - 1; index >= 0; index--)
                AttachStageTwoGeometry(item.GetChild(index), distantMap);
            return;
        }

        Renderer renderer = item.GetComponentInChildren<Renderer>(true);
        Collider collider = item.GetComponentInChildren<Collider>(true);
        Vector3 location = renderer != null ? renderer.bounds.center :
            collider != null ? collider.transform.position : item.position;
        if ((location.x > 150f) == distantMap)
            item.SetParent(mapRoot, true);
    }

    private Quaternion MapAngle(bool side) =>
        Quaternion.Euler(0f, side ? -90f : 0f, 0f);

    private void ApplyMapView(bool side)
    {
        Vector3 pivot = player.position;
        Vector3 localPivot = mapRoot.InverseTransformPoint(pivot);
        Quaternion angle = MapAngle(side);
        mapRoot.SetPositionAndRotation(pivot - angle * localPivot, angle);
        Physics.SyncTransforms();
    }

    private void StartMapTurn(bool side, Vector3 destination)
    {
        mapTurnStartPosition = mapRoot.position;
        mapTurnStartSide = isSideView;
        mapTurnPivot = player.position;
        mapTurnSource = mapRoot.InverseTransformPoint(mapTurnPivot);
        mapTurnOrigin = mapRoot.InverseTransformPoint(destination);
        mapTurnProgress = 0f;
        mapTurnStart = mapRoot.rotation;
        mapTurnTarget = MapAngle(side);
        mapTurnCameraOrigin = mapCameraPosition;
        mapTurnBody = player.GetComponent<Rigidbody>();
        if (mapTurnBody != null)
        {
            mapTurnBodyWasKinematic = mapTurnBody.isKinematic;
            if (!mapTurnBodyWasKinematic)
            {
                mapTurnBody.linearVelocity = Vector3.zero;
                mapTurnBody.angularVelocity = Vector3.zero;
                mapTurnBody.collisionDetectionMode = CollisionDetectionMode.Discrete;
                mapTurnBody.isKinematic = true;
                mapTurnBody.detectCollisions = false;
            }
        }
        isSideView = side;
        InputLockManager.CameraRotateLock = true;
        InputLockManager.Refresh();
        isRotating = true;
    }

    private void UpdateMapTurn()
    {
        mapTurnProgress = Mathf.MoveTowards(mapTurnProgress, 1f,
            Time.deltaTime * Mathf.Max(0.1f, rotationSpeed));
        bool done = mapTurnProgress >= 1f;
        float progress = Mathf.SmoothStep(0f, 1f, mapTurnProgress);
        Quaternion rotation = Quaternion.Slerp(mapTurnStart, mapTurnTarget, progress);
        Vector3 localPivot = Vector3.Lerp(mapTurnSource, mapTurnOrigin, progress);
        mapRoot.SetPositionAndRotation(mapTurnPivot - rotation * localPivot, rotation);
        Physics.SyncTransforms();
        mapCameraPosition = mapTurnCameraOrigin;
        ApplyMapCamera();
        if (done)
        {
            if (PlayerOverlapsMap())
            {
                mapRoot.SetPositionAndRotation(mapTurnStartPosition, mapTurnStart);
                isSideView = mapTurnStartSide;
                Physics.SyncTransforms();
                ApplyMapCamera();
            }
            FinishMapTurn();
        }
    }

    private bool PlayerOverlapsMap()
    {
        if (mapTurnBody == null)
            return false;

        foreach (CapsuleCollider capsule in player.GetComponents<CapsuleCollider>())
        {
            if (!capsule.enabled || capsule.isTrigger)
                continue;

            Vector3 center = capsule.transform.TransformPoint(capsule.center);
            Vector3 size = capsule.transform.lossyScale;
            Vector3 extents = new Vector3(capsule.radius * Mathf.Abs(size.x),
                capsule.height * 0.5f * Mathf.Abs(size.y), capsule.radius * Mathf.Abs(size.z));
            foreach (Collider other in Physics.OverlapBox(center, extents,
                         capsule.transform.rotation, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore))
            {
                if (!other.transform.IsChildOf(mapRoot))
                    continue;
                if (Physics.ComputePenetration(capsule, capsule.transform.position, capsule.transform.rotation,
                        other, other.transform.position, other.transform.rotation, out _, out float depth) &&
                    depth > 0.01f)
                    return true;
            }
        }

        return false;
    }

    private void FinishMapTurn()
    {
        if (mapTurnBody != null && !mapTurnBodyWasKinematic)
        {
            mapTurnBody.detectCollisions = true;
            mapTurnBody.isKinematic = false;
            mapTurnBody.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
        }
        mapTurnBody = null;
        isRotating = false;
        InputLockManager.CameraRotateLock = false;
    }

    public void FrameMapPuzzle(Vector3 position)
    {
        ApplyMapCamera(position);
    }

    private void ApplyMapCamera() => ApplyMapCamera(mapCameraPosition);

    private void ApplyMapCamera(Vector3 position)
    {
        transform.position = position;
        if (cameraBoundaryWalls == null || cameraBoundaryWalls.Length == 0)
            return;

        Vector3 interior = Vector3.zero;
        foreach (BoxCollider wall in cameraBoundaryWalls)
            interior += wall.transform.TransformPoint(wall.center);
        interior /= cameraBoundaryWalls.Length;

        Vector3 right = viewCamera.transform.right;
        Vector3 lens = viewCamera.transform.position;
        // Measure the screen at player depth, leaving the lens outside the set.
        float depth = Vector3.Dot(player.position - lens, viewCamera.transform.forward);
        Vector3 screenCenter = lens + viewCamera.transform.forward * depth;
        float halfWidth = viewCamera.orthographicSize * viewCamera.aspect;
        float minShift = float.NegativeInfinity;
        float maxShift = float.PositiveInfinity;

        foreach (BoxCollider wall in cameraBoundaryWalls)
        {
            Transform wallTransform = wall.transform;
            Vector3 center = wallTransform.TransformPoint(wall.center);
            Vector3 x = wallTransform.TransformVector(Vector3.right * wall.size.x);
            Vector3 y = wallTransform.TransformVector(Vector3.up * wall.size.y);
            Vector3 z = wallTransform.TransformVector(Vector3.forward * wall.size.z);
            Vector3 along = Vector3.ProjectOnPlane(x, Vector3.up);
            Vector3 horizontalY = Vector3.ProjectOnPlane(y, Vector3.up);
            Vector3 horizontalZ = Vector3.ProjectOnPlane(z, Vector3.up);
            if (horizontalY.sqrMagnitude > along.sqrMagnitude)
                along = horizontalY;
            if (horizontalZ.sqrMagnitude > along.sqrMagnitude)
                along = horizontalZ;
            Vector3 inward = Vector3.Cross(Vector3.up, along).normalized;
            if (Vector3.Dot(interior - center, inward) < 0f)
                inward = -inward;

            float lateral = Vector3.Dot(inward, right);
            if (Mathf.Abs(lateral) < 0.001f)
                continue;

            // Extend the inside face as a plane so short wall segments leave no camera gaps.
            float margin = 0.5f * (Mathf.Abs(Vector3.Dot(x, inward)) +
                Mathf.Abs(Vector3.Dot(y, inward)) + Mathf.Abs(Vector3.Dot(z, inward)));
            float edge = (margin - Vector3.Dot(screenCenter - center, inward)) / lateral;
            if (lateral > 0f)
                minShift = Mathf.Max(minShift, edge + halfWidth);
            else
                maxShift = Mathf.Min(maxShift, edge - halfWidth);
        }

        // Stop the frame inside the physical boundary so the player can walk
        // toward the edge of the screen without revealing the end of the set.
        if (!float.IsInfinity(minShift) && !float.IsInfinity(maxShift))
        {
            float inset = Mathf.Min(cameraEdgeInset, Mathf.Max(0f, (maxShift - minShift) * 0.5f));
            minShift += inset;
            maxShift -= inset;
        }
        else
        {
            minShift += cameraEdgeInset;
            maxShift -= cameraEdgeInset;
        }

        float shift = minShift <= maxShift
            ? Mathf.Clamp(0f, minShift, maxShift)
            : (minShift + maxShift) * 0.5f;
        transform.position += right * shift;
    }

    private Camera viewCamera;
    private float turnViewportX;
    private float turnHeight;
    private float turnStartAngle;
    private Vector3 turnStartMin;
    private Vector3 turnStartMax;

    public bool IsRotating => isRotating; // 외부 접근용

    [Header("Timing Puzzle")]
    public bool timingMode = false;
    public float normalSpeed = 5f;
    public float timingSpeed = 10.0f;

    [Header("Player Following Settings")]
    public PlayerMovement playerMovement;
    public Transform player; // 플레이어 Transform
    public float followSpeed = 3f; // 카메라가 플레이어를 따라가는 속도
    public Vector3 sideViewOffset = new Vector3(5f, 0f, 0f); // 사이드뷰에서의 오프셋
    private PlayerMovement3 playerMovement3;
    public GameObject playerObj;

    [Header("Stage 3 Camera")]
    public bool stage3CameraMode = false;
    public float stage3TiltAngle = 20f;
    public Vector3 stage3FollowOffset = new Vector3(0f, 1.8f, 0f);

    [Header("Movement Constraints - Current")]
    public bool useMovementConstraints = true;
    private Vector3 minPosition; // 현재 적용 중인 최소 범위
    private Vector3 maxPosition; // 현재 적용 중인 최대 범위
    
    [Header("Movement Constraints - Default View")]
    public Vector3 minPosDefault = new Vector3(-10f, 0f, -10f);
    public Vector3 maxPosDefault = new Vector3(10f, 0f, 10f);

    [Header("Movement Constraints - Side View")]
    public Vector3 minPosSide = new Vector3(-20f, 0f, -5f);
    public Vector3 maxPosSide = new Vector3(20f, 0f, 5f);
    

    public enum CameraMovingType { Fix, Moving, Follow } 
    public CameraMovingType cameraType = CameraMovingType.Fix;

    private Quaternion defaultRotation;       // 왼쪽 시점(기본)
    private Quaternion sideRotation;          // 오른쪽 시점
    private Quaternion targetRotation;        // 목표 회전값
    private Vector3 defaultPosition; // 기본 위치
    private Vector3 targetPosition; // 목표 위치
    private string nowstage;

    private float stage3Distance = 0; //3스테이지 카메라와 플레이어 거리

    public GameObject PipeManager;
    public bool PipeRun = false;

    public GameObject pipe;
    void Awake()
    {
        // 싱글톤 인스턴스 초기화
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
        
    }

    void OnEnable()
    {
        if (Instance == null) Instance = this;
    }

    private void OnDestroy()
    {
        if (tabHintFontAsset != null)
            Destroy(tabHintFontAsset);
    }

    void OnDisable()
    {
        if (mapRoot != null)
        {
            if (isRotating)
                FinishMapTurn();
            mapRoot.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
            Physics.SyncTransforms();
        }
        if (Instance == this) Instance = null;
    }

    void Start()
    {
        if (player != null)
        {
            playerMovement3 = player.GetComponent<PlayerMovement3>();
            //sideViewOffset = transform.position - player.position;
        }
            
        DecideCameraType();
        if (stage3CameraMode) { 
            isSideView = false;
            targetRotation = defaultRotation;
            transform.rotation = defaultRotation;

            SetConstraintsByView(); //위치제한설정
            if (stage3CameraMode && player != null) {   // 기본 시점(yaw 180°)에서 카메라와 플레이어의 Z 거리
            stage3Distance = Mathf.Abs(transform.position.z - player.position.z);
            }
            SnapToPlayer();//플레이어위치로
        
        }
        else
        {
            float currentY = transform.eulerAngles.y;
            if (Mathf.Abs(currentY - 270f) < 45f || Mathf.Abs(currentY + 90f) < 45f)
            {
                isSideView = true;
            }
            else
            {
                isSideView = false;
            }
            targetRotation = isSideView ? sideRotation : defaultRotation;

            SetConstraintsByView();
        }

        // sceneLoaded restores the position before Start builds the rotated map around it.
        if (ViewOnLoad == SavedCameraView.Front || ViewOnLoad == SavedCameraView.Side)
        {
            isSideView = ViewOnLoad == SavedCameraView.Side;
            targetRotation = isSideView ? sideRotation : defaultRotation;
            transform.rotation = targetRotation;
            SetConstraintsByView();
            ViewOnLoad = SavedCameraView.Unspecified;
        }

        SnapToPlayer();
        InitializeMapRotation();
        /* GameData data = DataManager.instance.nowGame;     
        nowstage = data.stage.ToString();
        PipeRun = PipeManager.GetComponent<BoardManager>().PipeRun;*/ //잠시주석처리0314

        //if (nowstage == "1")
        //{
        //    cameraType = CameraMovingType.Fix;
        //    defaultRotation = transform.rotation;
        //    sideRotation = Quaternion.Euler(0, 270, 0);
        //    targetRotation = defaultRotation;
        //}
        //else if (nowstage == "2")
        //{
        //    if (!PipeRun)
        //    {
        //        if (!isSideView) { cameraType = CameraMovingType.Fix; }
        //        else if (isSideView) { cameraType = CameraMovingType.Moving; }

        //    }
        //    else
        //    {
        //        cameraType = CameraMovingType.Fix;
        //        Debug.Log("Fix완료!");
        //    }


        //}
        //else if (nowstage == "3") //3스테이지 카메라 조건 정하기
        //{
        //    //if (!isSideView) { cameraType = CameraMovingType.Fix; }
        //    cameraType = CameraMovingType.Follow;
        //    //else if (isSideView) { cameraType = CameraMovingType.Follow; }
        //}
    }

    void Update()
    {
        //Debug.Log("현재 cameraType: " + cameraType);
        //Debug.Log("현재 스테이지: " + nowstage);
        // 이미 회전중이 아니고 잠겨있을 때만 입력 차단
        if (!isRotating && InputLockManager.LockCamera)
            return;
        // 현재 시점 반대 방향의 입력만 처리
        if (!isRotating &&
        InputManager.instance.GetKeyDown(isSideView ? KeyCode.E : KeyCode.Q) &&
        (cameraType == CameraMovingType.Fix ||
         cameraType == CameraMovingType.Moving))
        {
            HandleViewInput();
        }

    }

    void LateUpdate()
    {
        UpdateTutorialHint();
        if (mapRoot != null)
        {
            if (isRotating)
                UpdateMapTurn();
            else if (!InputLockManager.LockCamera)
            {
                Vector3 next = Vector3.Lerp(mapFollowPlayerPosition, player.position,
                    Time.deltaTime * followSpeed);
                Vector3 right = viewCamera.transform.right;
                Vector3 lensOffset = viewCamera.transform.position - transform.position;
                float centerError = Vector3.Dot(next - mapCameraPosition - lensOffset, right);
                mapCameraPosition += right * centerError;
                mapFollowPlayerPosition = next;
                ApplyMapCamera();
            }
            return;
        }

        if (!isRotating)
        {
            if (!InputLockManager.LockCamera)
                FollowPlayer();
            return;
        }

        turnRotation = Quaternion.Slerp(turnRotation, targetRotation,
            Time.deltaTime * rotationSpeed);
        bool finished = Quaternion.Angle(turnRotation, targetRotation) < 0.1f;
        transform.rotation = finished ? targetRotation : turnRotation;

        UpdateTurnPosition();

        if (finished)
        {
            isRotating = false;
            if (playerMovement != null)
                playerMovement.canMove = true;
            InputLockManager.CameraRotateLock = false;
        }
    }

    void DecideCameraType() {
        //GameData data = DataManager.instance.nowGame;
        //nowstage = data.stage.ToString();  //잠시주석0314
        nowstage = "1";

        defaultPosition = transform.position;
        targetPosition = defaultPosition;

        if (nowstage == "1")
        {
            cameraType = CameraMovingType.Moving;

            defaultRotation = Quaternion.Euler(0, 180, 0);
            sideRotation = Quaternion.Euler(0, 270, 0);
            isSideView = true;
            defaultPosition = transform.position;
            targetPosition = defaultPosition;
        }
        if (stage3CameraMode)
        {
            if (player != null)
            {
                playerMovement = player.GetComponent<PlayerMovement>();

                if (playerMovement != null)
                    playerMovement.enabled = true;
            }

            // 시점 전환 사용
            cameraType = CameraMovingType.Moving;
            isSideView = false;

            // 기본 시점: 위에서 약 20도 내려다보는 시점
            defaultRotation = Quaternion.Euler(20f, 180f, 0f);

            // 측면 시점: Y축으로 90도 전환
            sideRotation = Quaternion.Euler(20f, 270f, 0f);

            defaultPosition = transform.position;
            targetPosition = defaultPosition;
        }
        //else if (nowstage == "2") 
        //{
        //    if (!PipeRun)
        //    {
        //        if (!isSideView) { cameraType = CameraMovingType.Fix; }
        //        else if (isSideView) { cameraType = CameraMovingType.Moving; }

        //        defaultRotation = Quaternion.Euler(0, 180, 0);
        //        sideRotation = Quaternion.Euler(0, 270, 0);
        //        isSideView = false;

        //        defaultPosition = transform.position;
        //        targetPosition = defaultPosition;
        //    }
        //    else
        //    {
        //        cameraType = CameraMovingType.Fix;
        //        Debug.Log("Fix완료!");
        //    }
        //}
        //else if (nowstage == "3") //3스테이지 카메라 조건 정하기
        //{
        //    var playerscript1 = playerObj.GetComponent<PlayerMovement>();
        //    var playerscript3 = playerObj.GetComponent<PlayerMovement3>();
        //    playerscript1.enabled = true;
        //    playerscript3.enabled = false;
        //    //if (!isSideView) { cameraType = CameraMovingType.Fix; }
        //    cameraType = CameraMovingType.Fix;
        //    isSideView = false;
        //    defaultRotation = Quaternion.Euler(0, 180, 0);
        //    sideRotation = Quaternion.Euler(0, 270, 0);


        //    defaultPosition = transform.position;
        //    targetPosition = defaultPosition;
        //    //else if (isSideView) { cameraType = CameraMovingType.Follow; }
        //}
        //targetRotation = isSideView ? sideRotation : defaultRotation;
    }
    public void SnapToPlayer()
    {
        if (player == null)
            return;
        transform.position = GetFollowPosition(transform.rotation);
    }

    public Vector3 CapturePlayerFraming()
    {
        return Quaternion.Inverse(transform.rotation) * (player.position - transform.position);
    }

    public void TurnThroughPortal(bool sideView, Vector3 destination) => StartMapTurn(sideView, destination);

    public void RestorePortalView(bool sideView, Vector3 playerFraming)
    {
        BeginViewTurn(sideView, playerFraming);
        UpdateTurnPosition();
    }

    private void UpdateTurnPosition()
    {
        if (player == null)
            return;

        float remainingAngle = Quaternion.Angle(transform.rotation, targetRotation);
        float progress = turnStartAngle > 0.1f
            ? 1f - Mathf.Clamp01(remainingAngle / turnStartAngle)
            : 1f;
        Vector3 orbitPosition = player.position - transform.rotation * turnAnchor;
        Vector3 position = Vector3.Lerp(orbitPosition, GetFollowPosition(targetRotation), progress);

        float boundsProgress = Mathf.SmoothStep(0f, 1f, progress);
        Vector3 min = Vector3.Lerp(turnStartMin, minPosition, boundsProgress);
        Vector3 max = Vector3.Lerp(turnStartMax, maxPosition, boundsProgress);
        position.y = Mathf.Lerp(turnHeight, GetFollowPosition(targetRotation).y, boundsProgress);
        transform.position = ClampCameraPosition(position, min, max);

        if (viewCamera == null)
            return;

        Vector3 current = viewCamera.WorldToViewportPoint(player.position);
        if (current.z <= viewCamera.nearClipPlane)
            return;

        // Move the camera within its bounds so the view cannot shift past the clamp.
        Vector3 desired = new Vector3(turnViewportX, current.y, current.z);
        Vector3 correction = viewCamera.ViewportToWorldPoint(current) -
            viewCamera.ViewportToWorldPoint(desired);
        correction.y = 0f;
        transform.position = ClampCameraPosition(transform.position + correction * (1f - boundsProgress), min, max);
    }

    private float tutorialTabWait;
    private bool tutorialHintArmed;
    private bool tutorialHintUsed;
    private GameObject tutorialHint;
    [SerializeField] private Font tabHintFont;
    private TMPro.TMP_FontAsset tabHintFontAsset;

    private float tutorialTabDelay;

    public void ArmViewTutorialHint(float delay = 5f)
    {
        if (tutorialHintUsed)
            return;

        tutorialTabDelay = delay;
        tutorialTabWait = 0f;
        tutorialHintArmed = true;
    }

    private void UpdateTutorialHint()
    {
        if (InputLockManager.LockMove)
        {
            if (tutorialHint != null)
                Destroy(tutorialHint);
            return;
        }

        if (!tutorialHintArmed || tutorialHintUsed)
            return;

        tutorialTabWait += Time.deltaTime;
        if (tutorialTabWait < tutorialTabDelay)
            return;

        ShowViewTutorialHint("Q와 E 키로 시야를 전환해 보세요");
    }

    public void ShowViewTutorialHint(string message, float fontSizeReduction = 0f)
    {
        if (tutorialHintUsed || InputLockManager.LockMove)
            return;

        tutorialHintUsed = true;
        tutorialHint = new GameObject("View Tutorial Hint", typeof(Canvas));
        var canvas = tutorialHint.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 100;
        var label = StageChanger.AddLabel(canvas.transform, message,
            new Vector2(0f, -60f), new Vector2(750f, 70f));
        label.fontSize -= fontSizeReduction;
        if (tabHintFont != null)
        {
            if (tabHintFontAsset == null)
                tabHintFontAsset = TMPro.TMP_FontAsset.CreateFontAsset(tabHintFont);
            label.font = tabHintFontAsset;
        }
        label.rectTransform.anchorMin = label.rectTransform.anchorMax = new Vector2(0.5f, 0.9f);
        Destroy(tutorialHint, 4f);
    }

    private void HandleViewInput()
    {
        if (DialogueManager.Instance != null && DialogueManager.Instance.EndedThisFrame)
            return;

        if (!Teleporter.TryTeleport(player, this))
            TryRotateView();
    }

    public bool TryRotateView()
    {
        if (isRotating || InputLockManager.LockCamera ||
            (DialogueManager.Instance != null && DialogueManager.Instance.EndedThisFrame) ||
            (cameraType != CameraMovingType.Fix && cameraType != CameraMovingType.Moving)) return false;
        if (mapRoot != null)
            StartMapTurn(!isSideView, player.position);
        else
            BeginViewTurn(!isSideView, player != null ? CapturePlayerFraming() : Vector3.zero);
        if (tutorialHintArmed)
            tutorialHintUsed = true;
        if (tutorialHint != null)
            Destroy(tutorialHint);
        ViewTurnStarted?.Invoke();
        return true;
    }

    private void BeginViewTurn(bool sideView, Vector3 playerFraming)
    {
        if (viewCamera == null)
            viewCamera = GetComponentInChildren<Camera>();

        if (viewCamera != null && player != null)
        {
            Vector3 sourcePosition = transform.position + transform.rotation * playerFraming;
            turnViewportX = Mathf.Clamp(viewCamera.WorldToViewportPoint(sourcePosition).x, 0.08f, 0.92f);
        }

        turnHeight = transform.position.y;
        turnAnchor = playerFraming;
        turnRotation = transform.rotation;
        InputLockManager.CameraRotateLock = true;
        isSideView = sideView;
        targetRotation = isSideView ? sideRotation : defaultRotation;
        turnStartAngle = Quaternion.Angle(turnRotation, targetRotation);
        turnStartMin = minPosition;
        turnStartMax = maxPosition;
        isRotating = true;
        SetConstraintsByView();
        InputLockManager.Refresh();
    }

    public void RestoreView(bool sideView)
    {
        if (mapRoot != null)
        {
            FinishMapTurn();
            isSideView = sideView;
            ApplyMapView(sideView);
            mapFollowPlayerPosition = player.position;
            mapCameraPosition = player.position + transform.rotation * sideViewOffset;
            mapCameraPosition.y = minPosDefault.y;
            ApplyMapCamera();
            return;
        }

        isSideView = sideView;
        isRotating = false;
        targetRotation = isSideView ? sideRotation : defaultRotation;
        transform.rotation = targetRotation;
        SetConstraintsByView();
        SnapToPlayer();
        InputLockManager.CameraRotateLock = false;
    }
    private Vector3 GetFollowPosition(Quaternion rotation)
    {
        // 기울기(pitch)를 제외하고 Y축 회전만 사용
        Quaternion yawOnly = Quaternion.Euler(0f, rotation.eulerAngles.y, 0f);
        Vector3 rotatedOffset = yawOnly * sideViewOffset;
        Vector3 desiredPosition = player.position + rotatedOffset;
        
        if (stage3CameraMode)// 3스테이지: X, Y, Z 모두 플레이어를 따라감
        {
            desiredPosition.y = player.position.y + stage3FollowOffset.y; // Clamp 안 함
            if (isSideView)
                desiredPosition.x = player.position.x + stage3Distance;  // 측면: X 거리 유지, Z 따라감
            else
                desiredPosition.z = player.position.z + stage3Distance;  // 기본: Z 거리 유지, X 따라감
        }
        return ClampCameraPosition(desiredPosition, minPosition, maxPosition);
    }

    private Vector3 ClampCameraPosition(Vector3 position, Vector3 min, Vector3 max)
    {
        if (!useMovementConstraints)
            return position;

        position.x = Mathf.Clamp(position.x, min.x, max.x);
        position.z = Mathf.Clamp(position.z, min.z, max.z);
        if (!stage3CameraMode)
            position.y = Mathf.Clamp(position.y, min.y, max.y);

        return position;
    }

    void FollowPlayer()
    {
        if (player == null)
            return;

        Vector3 desiredPosition = GetFollowPosition(transform.rotation);
        transform.position = Vector3.Lerp(transform.position, desiredPosition, Time.deltaTime * followSpeed);
        if (Vector3.Distance(transform.position, desiredPosition) < 0.01f)
            transform.position = desiredPosition;
    }

    // 외부에서 움직임 범위를 설정할 수 있는 메서드
    public void SetMovementConstraints(Vector3 min, Vector3 max)
    {
            minPosition = min;
            maxPosition = max;
    }


    public Vector3 cameraOffset;      // 카메라와 플레이어 사이 거리
    
        // 외부에서 플레이어 따라가기 설정을 변경할 수 있는 메서드
    public void SetFollowSettings(float speed, Vector3 offset)
    {
            followSpeed = speed;
            sideViewOffset = offset;
    }
    // [추가] 시점에 따라 범위를 갈아끼워주는 전용 함수
    void SetConstraintsByView()
    {
        if (isSideView)
        {
            minPosition = minPosSide;
            maxPosition = maxPosSide;
        }
        else
        {
            minPosition = minPosDefault;
            maxPosition = maxPosDefault;
        }
    }
    
    void FreeCircleFollowPlayer()
    {
        if (player == null || playerMovement3 == null || playerMovement3.center == null)
            return;

        // 중심 기준 벡터
        Vector3 center = playerMovement3.center.position;
        Vector3 toPlayer = player.position - center;

        float playerRadius = toPlayer.magnitude;
        float cameraRadius = playerRadius + 3f; // 카메라가 플레이어보다 더 멀리 떨어지도록

        // 플레이어가 있는 각도 계산 (XZ 평면)
        float angleRad = Mathf.Atan2(toPlayer.z, toPlayer.x);

        // 카메라 위치 = 같은 각도, 더 큰 반지름
        float x = center.x + cameraRadius * Mathf.Cos(angleRad);
        float z = center.z + cameraRadius * Mathf.Sin(angleRad);
        float y = player.position.y + 1f; 

        Vector3 desiredPosition = new Vector3(x, y, z);

        float t = 1f - Mathf.Exp(-10f * Time.deltaTime); // 빠른 지수 보간
        transform.position = Vector3.Lerp(transform.position, desiredPosition, t);

        Vector3 lookAtPos = player.position;
        lookAtPos.y = transform.position.y; // 현재 카메라 높이로 고정 (X, Z축 회전 방지)
        transform.LookAt(lookAtPos);
    }
    //void FreeFollowPlayer()
    //{
    //    if (player == null) return;

    //    // 플레이어 → 중심 방향 계산
    //    Vector3 playerToCenter = player.position - playerMovement.center.position;
    //    Vector3 offsetDirection = Quaternion.Euler(0f, 90f, 0f) * playerToCenter.normalized; // 90도 회전해서 옆에서 보기

    //    float distance = 5f; // 플레이어와 카메라 거리
    //    float height = 0.1f;   // 카메라 높이

    //    // 최종 위치 계산 (원 궤도 따라 회전)
    //    Vector3 desiredPosition = player.position + offsetDirection * distance + Vector3.up * height;

    //    transform.position = Vector3.Lerp(transform.position, desiredPosition, followSpeed * Time.deltaTime);
    //    //transform.LookAt(player.position + Vector3.up * 1f); // 약간 위를 바라보게
    //    Debug.Log("FreeFollow target position: " + desiredPosition);
    //    Debug.Log("Camera position before move: " + transform.position);
    //}
}
