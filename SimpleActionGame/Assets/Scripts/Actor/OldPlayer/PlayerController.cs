using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;

using Actor.Player;

/// <summary>
/// キャラクターを動かす元となるクラス
/// 
/// 操作方法
/// 十字キー右，左で移動
/// 高速で二回移動キーを押すとダッシュ
/// スペースでジャンプ
/// 
/// このクラスは触らなくても大丈夫
/// 勝手にやってくれる (いじりたければどうぞ)
/// Data(実質グローバル変数)には，変数付け加えてもいいかも
/// 
/// DataのVelocityを各ステートクラスでいじると，
/// その速度に応じて座標移動してくれる
/// 
/// 今回のUnityのInspectorで変数をいじれない仕様になってるのは許して
/// 
/// </summary>

namespace Actor.Player
{
    // プレイヤーのステート一覧
    public enum eState
    {
        Wait, // 待機中のステート アイドリング
        Walk, // 歩いているステート
        Run, // 走っているステート
        Jump, // ジャンプ中のステート
        Fall, // 落下中のステート(ジャンプでの落下はこれじゃない)
    }

    // プレイヤーのデータ 実質ステート間のグローバル変数
    //[System.Serializable]
    public class PlayerData
    {
        // 中途半端にプロパティにしてしまった

        // プレイヤーの速度
        public Vector2 velocity = Vector2.zero;
        // 接地しているかどうか
        public bool IsGround { private set; get; }
        // 天井に頭がぶつかっているかどうか
        public bool IsHead { private set; get; } // 変数名が思いつかない
        // 前回のステート
        public eState PrevState { private set; get; }

        // ステートを変更したいときに，要求する列挙型を入れる
        public Queue<eState> state_queue;

        public Animator animator;

        // コンストラクタ
        public PlayerData()
        {
            IsGround = false;
            IsHead = false;
            PrevState = eState.Fall;
            state_queue = new Queue<eState>();
        }

        // 以下，それぞれの変数を代入
        public void SetIsGround(bool is_ground) { IsGround = is_ground; }
        public void SetIsHead(bool is_head) { IsHead = is_head; }
        public void SetPrevState(eState prev_state) { PrevState = prev_state; }
    }

    [RequireComponent(typeof(BoxCollider2D))] // BoxCollider2Dがアタッチされていないといけない
    [RequireComponent(typeof(Animator))]
    public class PlayerController : MonoBehaviour
    {
        // プレイヤー全体のデータ
        //[SerializeField]
        private PlayerData data_;

        // 現在のステート
        private StateBase state_;

        // シーン名とそれに対応するシーンクラスの辞書
        private Dictionary<eState, StateBase> factory_;

        private BoxCollider2D hit_box_;

        private const float kEpsilon = 0.001f;

        // Start is called before the first frame update
        private void Start()
        {
            data_ = new PlayerData();
            //data_.animator = GetComponent<Animator>();

            factory_ = new Dictionary<eState, StateBase>();

            hit_box_ = GetComponent<BoxCollider2D>();

            // 始めはFallステート
            state_ = new StateFall();
            state_.OnStart(data_);


            RegisterState();
        }

        // Update is called once per frame
        private void Update()
        {
            //// 接地判定，天井に頭がついているかの判定をする
            //CheckIsGround(); CheckIsHead();

            // 更新
            state_.Proc(data_);

            // ステートを変更するかどうか
            CheckState();

            // 速度に応じて座標を変更 
            // ついでに接地判定，天井に頭がついているかの判定も行う
            Move();
            //transform.position += (Vector3)data_.velocity * Time.deltaTime * 60f;
        }

        // ステートを登録
        private void RegisterState()
        {
            factory_.Add(eState.Wait, new StateWait());
            factory_.Add(eState.Walk, new StateWalk());
            factory_.Add(eState.Run, new StateRun());
            factory_.Add(eState.Jump, new StateJump());
            factory_.Add(eState.Fall, new StateFall());

        }

        // ステート変更
        private void CheckState()
        {
            while (data_.state_queue.Count > 1)
            {
                Assert.IsTrue(factory_.ContainsKey(data_.state_queue.Peek()), "登録されていないステートです");
                data_.SetPrevState(data_.state_queue.Dequeue()); // 現在のステートを保持
                state_.OnEnd(data_); // 現在のステートの終了処理
                Debug.Log(data_.state_queue.Peek());
                state_ = factory_[data_.state_queue.Peek()]; // 新しいステートに変更
                state_.OnStart(data_); // 新しいステートの初期化
            }
        }


        // 座標を変更する
        private void Move()
        {
            // 壁にめり込まないように移動する x軸だけ 坂道にまったく対応できてない

            Vector2 scale = transform.localScale;

            // 当たり判定(矩形)のサイズと中心
            Vector2 offset = hit_box_.offset * scale;
            Vector2 half_size = hit_box_.size * scale * 0.5f;

            // 移動量
            Vector2 d = data_.velocity * Time.deltaTime * 60f;

            // レイキャストを飛ばす
            Vector2 origin = (Vector2)transform.position + offset;

            int mask = 1 << 8 | 1 << 9;

            // まずはx軸方向
            if (d.x < 0)
            {
                RaycastHit2D hit_left = Physics2D.BoxCast(origin, new Vector2(half_size.x, half_size.y * 0.6f), 0f, Vector2.left,
                    -d.x + half_size.x / 2f, mask);
                Debug.DrawLine(origin, origin - new Vector2(half_size.x - d.x, 0f), Color.blue);
                if (hit_left)
                {
                    d = new Vector2(-hit_left.distance + half_size.x / 2f, d.y);
                    Debug.Log("左方向あたり");
                }
            }
            else if(d.x > 0)
            {
                RaycastHit2D hit_right = Physics2D.BoxCast(origin, new Vector2(half_size.x, half_size.y * 0.6f), 0f, Vector2.right,
                    d.x + half_size.x / 2f, mask);
                Debug.DrawLine(origin, origin + new Vector2(half_size.x + d.x, 0f), Color.blue);
                if (hit_right)
                {
                    d = new Vector2(hit_right.distance - half_size.x / 2f, d.y);
                    Debug.Log("右方向あたり");
                }
            }

            // x軸移動
            origin += new Vector2(d.x, 0f);

            // 次にy軸方向 ここでは，3本の線を出す 坂道チェックもする
            // まずは下方向
            {
                // ヒットした場合は一番高いのに合わせる
                float new_d_y = d.y;
                RaycastHit2D hit_down_center = LinecastWithGizmos(origin, origin + new Vector2(0f, -half_size.y + d.y - kEpsilon), mask);
                float center_d_y = -100f;
                if (hit_down_center)
                {
                    center_d_y = -(hit_down_center.distance - half_size.y);
                    //Debug.Log("下方向あたり(center)");
                }

                Vector2 origin_left = origin + new Vector2(-half_size.x * 0.6f, 0f);
                RaycastHit2D hit_down_left = LinecastWithGizmos(origin_left, origin_left + new Vector2(0f, -half_size.y + d.y - kEpsilon), mask);
                float left_d_y = -100f;
                if (hit_down_left)
                {
                    left_d_y =  -(hit_down_left.distance - half_size.y);
                    //Debug.Log("下方向あたり(left)");
                }

                Vector2 origin_right = origin + new Vector2(half_size.x * 0.6f, 0f);
                RaycastHit2D hit_down_right = LinecastWithGizmos(origin_right, origin_right + new Vector2(0f, -half_size.y + d.y - kEpsilon), mask);
                float right_d_y = -100f;
                if (hit_down_right)
                {
                    right_d_y = -(hit_down_right.distance - half_size.y);
                    //Debug.Log("下方向あたり(right)");
                }

                //if(hit_down_center && hit_down_left && hit_down_right)
                //{
                //    // 坂道であるか確かめる
                //    new_d_y = center_d_y;
                //}

                new_d_y = Mathf.Max(d.y, center_d_y, left_d_y, right_d_y);

                d = new Vector2(d.x, new_d_y);

                if (hit_down_center || hit_down_left || hit_down_right) data_.SetIsGround(true);
                else data_.SetIsGround(false);
            }

            // 下向き方向にヒットしたならば，上向き方向は行わない
            if (!data_.IsGround)
            {
                // ヒットした場合は一番高いのに合わせる
                float new_d_y = d.y;
                RaycastHit2D hit_up_center = LinecastWithGizmos(origin, origin + new Vector2(0f, half_size.y + d.y + kEpsilon), mask);
                if (hit_up_center)
                {
                    new_d_y = Mathf.Min(new_d_y, hit_up_center.distance - half_size.y);
                    //Debug.Log("上方向あたり(center)");
                }

                Vector2 origin_left = origin + new Vector2(-half_size.x * 0.6f, 0f);
                RaycastHit2D hit_up_left = LinecastWithGizmos(origin_left, origin_left + new Vector2(0f, half_size.y + d.y + kEpsilon), mask);
                if (hit_up_left)
                {
                    new_d_y = Mathf.Min(new_d_y, hit_up_left.distance - half_size.y);
                    //Debug.Log("上方向あたり(left)");
                }

                Vector2 origin_right = origin + new Vector2(half_size.x * 0.6f, 0f);
                RaycastHit2D hit_up_right = LinecastWithGizmos(origin_right, origin_right + new Vector2(0f, half_size.y + d.y + kEpsilon), mask);
                if (hit_up_right)
                {
                    new_d_y = Mathf.Min(new_d_y, hit_up_right.distance - half_size.y);
                    //Debug.Log("上方向あたり(right)");
                }

                d = new Vector2(d.x, new_d_y);

                if (hit_up_center || hit_up_left || hit_up_right) data_.SetIsHead(true);
            }

            transform.position += (Vector3)d;
        }

        // レイキャストを飛ばす(Debugの線を引く)
        RaycastHit2D LinecastWithGizmos(Vector2 from, Vector2 to, int layer_mask)
        {
            RaycastHit2D hit = Physics2D.Linecast(from, to, layer_mask);
            Debug.DrawLine(from, (hit) ? to : to);
            return hit;
        }
    }
}