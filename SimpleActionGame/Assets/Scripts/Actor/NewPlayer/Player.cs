using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TadaLib; // ステートマシンを使うため

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
/// それと，各ステートでPlayerクラスのprivate変数をいじれるようになってる 内部クラスは外部クラスの変数をすべて見れる
/// 
/// </summary>

namespace Actor.NewPlayer
{
    // ステート間で共有するデータ
    public class Data
    {
        // プレイヤーの速度
        public Vector2 velocity = Vector2.zero;
        // 接地しているかどうか
        public bool IsGround { private set; get; }
        // 天井に頭がぶつかっているかどうか
        public bool IsHead { private set; get; } // 変数名が思いつかない

        // アニメーター
        public Animator animator;

        // それぞれのステートのデータ

        // コンストラクタ
        public Data()
        {
            IsGround = false;
            IsHead = false;
        }

        // 以下，それぞれの変数を代入
        public void SetIsGround(bool is_ground) { IsGround = is_ground; }
        public void SetIsHead(bool is_head) { IsHead = is_head; }
    }

    // プレイヤークラス partialによりファイル間で分割してクラスを実装
    [RequireComponent(typeof(BoxCollider2D))] // BoxCollider2Dがアタッチされていないといけない
    [RequireComponent(typeof(Animator))]
    public partial class Player : MonoBehaviour
    {
        // プレイヤーのステート一覧
        private enum eState
        {
            Wait, // 待機中のステート アイドリング
            Walk, // 歩いているステート
            Run, // 走っているステート
            Jump, // ジャンプ中のステート
            Fall, // 落下中のステート(ジャンプでの落下はこれじゃない)
        }

        // ステートマシン
        private StateMachine<Player> state_machine_;

        // ステート間で共有するデータ
        private Data data_;

        // 各ステートのインスタンス これで各ステートのフィールドをInspectorでいじれる 神
        [SerializeField]
        private StateIdle idle_state_;
        [SerializeField]
        private StateWalk walk_state_;
        [SerializeField]
        private StateRun run_state_;
        [SerializeField]
        private StateJump jump_state_;
        [SerializeField]
        private StateFall fall_state_;

        // 当たり判定の範囲
        private BoxCollider2D hit_box_;

        private const float kEpsilon = 0.001f;

        // Start is called before the first frame update
        private void Start()
        {
            data_ = new Data
            {
                animator = GetComponent<Animator>()
            };

     
            hit_box_ = GetComponent<BoxCollider2D>();

            // ステートマシンのメモリ確保 自分自身を渡す
            state_machine_ = new StateMachine<Player>(this);
            // ステートを登録
            state_machine_.AddState((int)eState.Wait, idle_state_);
            state_machine_.AddState((int)eState.Walk, walk_state_);
            state_machine_.AddState((int)eState.Run, run_state_);
            state_machine_.AddState((int)eState.Jump, jump_state_);
            state_machine_.AddState((int)eState.Fall, fall_state_);

            // 始めのステートを設定
            state_machine_.SetInitialState((int)eState.Fall);
        }

        // Update is called once per frame
        private void Update()
        {
            // 状態を更新する
            state_machine_.Proc();

            // 向いている方向を正しくする
            CheckFaceDirChange();

            // 速度に応じて移動する
            Move();
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
            else if (d.x > 0)
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
                    left_d_y = -(hit_down_left.distance - half_size.y);
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

        // 方向転換するか確かめる
        private void CheckFaceDirChange()
        {
            //// 速度がある方向を向く 速度が0の場合はそのまま
            //if(data_.velocity.x < 0f)
            if (Input.GetKey(KeyCode.LeftArrow))
            {
                if (transform.localEulerAngles.y != 180f)
                    transform.localEulerAngles = new Vector3(transform.localEulerAngles.x, 180f, transform.localEulerAngles.z);
            }
            // else if(data_.velocity.x > 0f)
            else if (Input.GetKey(KeyCode.RightArrow))
            {
                if (transform.localEulerAngles.y != 0f) // 浮動小数点型で==はあんまよくないけど・・・
                    transform.localEulerAngles = new Vector3(transform.localEulerAngles.x, 0f, transform.localEulerAngles.z);
            }
        }

        // レイキャストを飛ばす(+ Debugの線を引く)
        RaycastHit2D LinecastWithGizmos(Vector2 from, Vector2 to, int layer_mask)
        {
            RaycastHit2D hit = Physics2D.Linecast(from, to, layer_mask);
            Debug.DrawLine(from, (hit) ? to : to);
            return hit;
        }
    }
}