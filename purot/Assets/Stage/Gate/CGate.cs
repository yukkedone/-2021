/*==============================================================================
    Project
    [CGate.cs]
    ・ゲート制御
--------------------------------------------------------------------------------
    2021.04.25 @Author Suzuki Hayase
================================================================================
    History
        2021.05.09 @Author Misaki Sasaki 
            125~127行目を見てください。そっと、スコアをAddする処理追加してます。
        2021.5.15 @Author Misaki Sasaki 
            36~,52~,156~,169~行目。オーダー失敗/成功時に赤い/緑パネルが出るようにしました。
            
/*============================================================================*/
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CGate : MonoBehaviour {
    private int iPassNum;                             // 通過数
    private int iMatchNum;                            // 一致数
    private int iClearNum;                            // クリア数
    private COrderManager csOrderManager;             // OrderManagerスクリプト

    [SerializeField] private GameObject gClear;       // クリアスタンプ

    private float fRadius = 9f;        // 回転半径
    private float fSpeed = 0.5f;       // 回転速度
    private float fDegree;             // 角度
    private RotateState State;         // オブジェクトの回転状態

    private GameObject gGateTimerController;            // LampManagerのオブジェクトを格納
    private CGateTimerController csGateTimerController; // ゲートタイマースクリプト

    //-- 2021.5.15追加 sasaki
    [SerializeField] private GameObject gPanelObjectRed;    // パネルプレハブ（赤）
    [SerializeField] private GameObject gPanelObjectGreen;  // パネルプレハブ（緑）
    private GameObject gPanelRed;    // 生成するパネル本体（赤）
    private GameObject gPanelGreen;  // 生成するパネル本体（緑）

    private GameObject gCanvas; // パネルの親にしたいキャンバス

    // Start is called before the first frame update
    void Start() {
        iMatchNum = 0;
        iClearNum = 0;
        gGateTimerController = GameObject.Find("PFB_GateTimerController");
        csOrderManager = GameObject.Find("PFB_OrderManager").GetComponent<COrderManager>();
        csGateTimerController = GameObject.Find("PFB_GateTimerController").GetComponent<CGateTimerController>();

        //-- 2021.5.15追加 sasaki
        gCanvas = GameObject.Find("PFB_Canvas");
        Quaternion rot = Quaternion.Euler(45.0f, 0.0f, 0.0f);
        gPanelRed = (GameObject)Instantiate(gPanelObjectRed, new Vector3(0.0f, 0.0f, 0.0f), rot);
        gPanelRed.GetComponent<RectTransform>().sizeDelta = new Vector2(1920.0f, 1080.0f);
        gPanelRed.gameObject.transform.parent = gCanvas.gameObject.transform;
        gPanelRed.SetActive(false);

        gPanelGreen = (GameObject)Instantiate(gPanelObjectGreen, new Vector3(0.0f, 0.0f, 0.0f), rot);
        gPanelGreen.GetComponent<RectTransform>().sizeDelta = new Vector2(1920.0f, 1080.0f);
        gPanelGreen.gameObject.transform.parent = gCanvas.gameObject.transform;
        gPanelGreen.SetActive(false);
    }

    // Update is called once per frame
    void Update() {

        // 角度加算
        fDegree -= fSpeed;

        Vector3 pos;

        // 位置更新
        pos.x = fRadius * Mathf.Sin((fDegree + 180) * Mathf.Deg2Rad);
        pos.y = 0.0f;
        pos.z = fRadius * Mathf.Cos((fDegree + 180) * Mathf.Deg2Rad);
        transform.position = pos;

        // 一周したら内側、外側を入れ替える処理
        // 角度が360を超えたら裏表を変えて、角度変数を0にする
        if (fDegree <= 0 && State == RotateState.OUTSIDE) {
            fDegree = 720.0f;
            Set_State(RotateState.INSIDE);
        }
        else if (fDegree <= 360 && State == RotateState.INSIDE) {
            Set_State(RotateState.OUTSIDE);
        }

        gGateTimerController.transform.LookAt(this.transform);
    }

    public void Set_fDegree(float d) {
        fDegree = d;
    }

    // 回転ステート変更関数setter
    public void Set_State(RotateState newState) {
        State = newState;
        switch (State) {
            case RotateState.INSIDE:
                //内側回転用の処理
                fRadius = 5.0f;
                if (fDegree < 360.0f) {
                    fDegree += 360.0f;
                }
                break;
            case RotateState.OUTSIDE:
                //外側回転用処理
                fRadius = 9.0f;
                if (fDegree > 360.0f) {
                    fDegree -= 360.0f;
                }
                break;
            default: break;
        }
    }


    void OnTriggerEnter(Collider col) {
        // 通過オブジェクト判定
        if (col.gameObject.tag == "RotateObject") {
            GameObject lamp = csOrderManager.Get_gClearLamp(iPassNum);
            OBJECT_SHAPE order = csOrderManager.Get_Order(iPassNum);

            // 指令と一致
            if (order == col.gameObject.GetComponent<CRotateObject>().Get_Shape()) {
                iMatchNum++;
                lamp.GetComponent<Renderer>().material.color = Color.green;
            }
            else {
                lamp.GetComponent<Renderer>().material.color = Color.red;
            }

            // 通過したオブジェクトを削除
            CObjectManager.Instance.Remove(col.gameObject);
            CObjectManager.Instance.AcceleRemove(col.gameObject);
            Destroy(col.gameObject);
            Debug.Log("hit");

            iPassNum++;

            int ordernum = csOrderManager.Get_iOrderNum();

            // 指令数のオブジェクトが通過したらリセット
            if (iPassNum == ordernum)
            {
                if (iMatchNum == ordernum)
                {
                    // クリアスタンプ生成
                    //Instantiate(gClear, new Vector3(20, 0, -10 + iClearNum * 5),
                    //    Quaternion.Euler(0, 180, 0));

                    //========== 2021/5/09
                    // スコアを記録するのに必要なので足しました　by佐々木
                    CScore.AddScore();
                    //-- 2021.5.15追加 sasaki
                    gPanelGreen.SetActive(true);

                    // 指令生成
                    csOrderManager.CreateOrder(3);
                    iClearNum++;
                }
                else {
                    for (int i = 0; i < ordernum; i++) {
                        GameObject l = csOrderManager.Get_gClearLamp(i);
                        l.GetComponent<Renderer>().material.color = Color.white;
                    }
                    //-- 2021.5.15追加 sasaki
                    gPanelRed.SetActive(true);
                }
                // 新しいオブジェクト生成
                CObjectManager.Instance.Create(3);

                csGateTimerController.Reset();
                gGateTimerController.transform.LookAt(new Vector3(0,0,10));
                Destroy(this.gameObject);
            }
        }
    }
}
