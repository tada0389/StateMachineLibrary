using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TadaLib;
using Actor;

namespace Actor.Enemy
{
    [RequireComponent(typeof(BoxCollider2D))] // BoxCollider2Dがアタッチされていないといけない
    public class EnemyController : MonoBehaviour
    {
        private enum eState
        {
            Wait, // 待機中
            MoveAhead, // 指定方向に進む
            Crash, // 何かにぶつかる
            MoveReturn, // ぶつかった後に戻る
        }

        // どの方向に進むか
        private enum eMoveDirection
        {
            Left = 0,
            Up = 1,
            Right = 2,
            Down = 3,
        }

        private StateMachine<EnemyController> state_machine_;

        // 速度
        private Vector2 velocity;
        // 加速度
        private Vector2 accel;
        [SerializeField]
        private eMoveDirection dir;
        // 移動先で衝突したかどうか
        private bool IsHit = false;

        // プレイヤーの位置
        [SerializeField]
        private Transform player_transform;

        // デフォルトの座標
        private Vector3 default_pos;

        // 当たり判定の範囲
        private BoxCollider2D hit_box;

        // 移動方向の列挙型に対応するベクトルを返す
        private Vector2[] DirToVector = { Vector2.left, Vector2.up, Vector2.right, Vector2.down };

        // Start is called before the first frame update
        private void Start()
        {
            hit_box = GetComponent<BoxCollider2D>();

            state_machine_ = new StateMachine<EnemyController>(this);

            state_machine_.AddState((int)eState.Wait, new StateWait());
            state_machine_.AddState((int)eState.MoveAhead, new StateMoveAhead());
            state_machine_.AddState((int)eState.Crash, new StateCrash());
            state_machine_.AddState((int)eState.MoveReturn, new StateMoveReturn());

            state_machine_.SetInitialState((int)eState.Wait);
        }

        // Update is called once per frame
        private void Update()
        {
            state_machine_.Proc();

            Move();
        }

        // 実際に移動する
        private void Move()
        {
            Vector2 scale = transform.localScale;

            // 当たり判定(矩形)のサイズと中心
            Vector2 offset = hit_box.offset * scale;
            Vector2 half_size = hit_box.size * scale * 0.5f;

            // 移動量
            Vector2 d = velocity * Time.deltaTime * 60f;

            // レイキャストを飛ばす
            Vector2 origin = (Vector2)transform.position + offset;

            IsHit = false;

            // 上下左右にボックスキャストを飛ばす
            int mask = 1 << 8;

            // まずは左右方向
            {
                // 左方方向
                RaycastHit2D hit_left = Physics2D.BoxCast(origin, new Vector2(half_size.x, half_size.y * 0.6f), 0f, Vector2.left,
                    -d.x + half_size.x / 2f, mask);
                if (hit_left)
                {
                    d = new Vector2(-hit_left.distance + half_size.x / 2f, d.y);
                    IsHit = (dir == eMoveDirection.Left);
                    //Debug.Log("左方向あたり");
                }
                else
                {
                    // 右方向もやる
                    RaycastHit2D hit_right = Physics2D.BoxCast(origin, new Vector2(half_size.x, half_size.y * 0.6f), 0f, Vector2.right,
                        d.x + half_size.x / 2f, mask);
                    Debug.DrawLine(origin, origin + new Vector2(half_size.x + d.x, 0f), Color.blue);
                    if (hit_right)
                    {
                        d = new Vector2(hit_right.distance - half_size.x / 2f, d.y);
                        IsHit = (dir == eMoveDirection.Right);
                        //Debug.Log("右方向あたり");
                    }
                }
            }

            // x軸移動
            origin += new Vector2(d.x, 0f);
            
            // 次に上下方向
            {
                // 上方向
                RaycastHit2D hit_up = Physics2D.BoxCast(origin, new Vector2(half_size.x * 0.6f, half_size.y), 0f, Vector2.up,
                    d.y + half_size.y / 2f, mask);
                if (hit_up)
                {
                    d = new Vector2(d.x, hit_up.distance - half_size.y / 2f);
                    if (dir == eMoveDirection.Up) IsHit = true;
                    //Debug.Log("上方向あたり");
                }
                else
                {
                    // 下方向
                    RaycastHit2D hit_down = Physics2D.BoxCast(origin, new Vector2(half_size.x * 0.6f, half_size.y), 0f, Vector2.down,
                        -d.y + half_size.y / 2f, mask);
                    if (hit_down)
                    {
                        d = new Vector2(d.x, -hit_down.distance + half_size.y / 2f);
                        if (dir == eMoveDirection.Down) IsHit = true;
                        //Debug.Log("下方向あたり");
                    }
                }
            }

            // 速度を座標に反映
            transform.position += (Vector3)d;
        }

        // ファイルを分割しなくてもかける
        private class StateWait : StateMachine<EnemyController>.StateBase
        {
            private float left, right, top, bottom; // ドッスンが移動する判定

            public override void OnStart()
            {
                DumpStartMsg(this);
                Parent.accel = Vector2.zero;
                Parent.velocity = Vector2.zero;

                SetSearchScale();
            }
            public override void OnEnd()
            {
                DumpEndMsg(this);
            }

            public override void Proc()
            {
                if (SearchObject(Parent.player_transform.position)) ChangeState((int)eState.MoveAhead);
            }

            // 対象が移動方向にいるかどうか
            private bool SearchObject(Vector3 target)
            {
                // それぞれの方向に落ちるかどうか
                switch (Parent.dir)
                {
                    case eMoveDirection.Down:
                        return IsExistVertical(target) && Parent.transform.position.y >= target.y;
                    case eMoveDirection.Up:
                        return IsExistVertical(target) && Parent.transform.position.y <= target.y;
                    case eMoveDirection.Left:
                        return IsExistHorizontal(target) && Parent.transform.position.x >= target.x;
                    case eMoveDirection.Right:
                        return IsExistHorizontal(target) && Parent.transform.position.x <= target.x;
                    default:
                        return false;
                }
            }

            // 対象が左右の範囲内にいるかどうか
            private bool IsExistVertical(Vector3 target)
            {
                return left <= target.x && target.x <= right;
            }
            // 対象が上下の範囲内にいるかどうか
            private bool IsExistHorizontal(Vector3 target)
            {
                return bottom <= target.y && target.y <= top;
            }

            // ドッスンの移動検知判定を決める 名前思いつかん
            private void SetSearchScale()
            {
                if (Parent.dir == eMoveDirection.Down || Parent.dir == eMoveDirection.Up)
                {
                    left = Parent.transform.position.x - Parent.hit_box.size.x * Parent.transform.localScale.x * 1.2f;
                    right = Parent.transform.position.x + Parent.hit_box.size.x * Parent.transform.localScale.x * 1.2f;
                }
                else
                {
                    top = Parent.transform.position.y + Parent.hit_box.size.y * Parent.transform.localScale.y * 1.2f;
                    bottom = Parent.transform.position.y - Parent.hit_box.size.y * Parent.transform.localScale.y * 1.2f;
                }
            }
        }

        private class StateMoveAhead : StateMachine<EnemyController>.StateBase
        {
            private const float kMaxSpeed = 0.5f;

            public override void OnStart()
            {
                DumpStartMsg(this);
                Parent.accel = Parent.DirToVector[(int)Parent.dir] * 0.02f;
                Parent.velocity = Vector2.zero;

                // 落下開始地点を保存
                Parent.default_pos = Parent.transform.position;
            }
            public override void OnEnd()
            {
                DumpEndMsg(this);
            }

            public override void Proc()
            {
                // もしステージに衝突したなら次のステートへ
                if (Parent.IsHit)
                {
                    ChangeState((int)eState.Crash);
                    return;
                }

                // 速度を変更
                ActorUtils.AddAccel(ref Parent.velocity, Parent.accel * Time.deltaTime * 60f, Vector2.one * kMaxSpeed);
            }
        }

        private class StateMoveReturn : StateMachine<EnemyController>.StateBase
        {
            private const float kMaxSpeed = 0.1f;

            public override void OnStart()
            {
                DumpStartMsg(this);

                Parent.accel = Parent.DirToVector[(int)Parent.dir] * -0.005f;
                Parent.velocity = Vector2.zero;
            }
            public override void OnEnd()
            {
                DumpEndMsg(this);
            }

            public override void Proc()
            {
                // 初期座標に戻ったなら終了
                bool change = false;
                switch (Parent.dir)
                {
                    case eMoveDirection.Down:
                        change = Parent.transform.position.y >= Parent.default_pos.y;
                        break;
                    case eMoveDirection.Up:
                        change = Parent.transform.position.y <= Parent.default_pos.y;
                        break;
                    case eMoveDirection.Left:
                        change = Parent.transform.position.x >= Parent.default_pos.x;
                        break;
                    case eMoveDirection.Right:
                        change = Parent.transform.position.x <= Parent.default_pos.x;
                        break;
                    default:
                        change = false;
                        break;
                }
                if(change)
                {
                    ChangeState((int)eState.Wait);
                    return;
                }

                // 速度を変更
                ActorUtils.AddAccel(ref Parent.velocity, Parent.accel * Time.deltaTime * 60f, Vector2.one * kMaxSpeed);
            }
        }

        private class StateCrash : StateMachine<EnemyController>.StateBase
        {
            // ある程度たったら戻る
            private float timer;

            public override void OnStart()
            {
                DumpStartMsg(this);
                Parent.accel = Vector2.zero;
                Parent.velocity = Vector2.zero;

                timer = 0.0f;
            }
            public override void OnEnd()
            {
                DumpEndMsg(this);
            }

            public override void Proc()
            {
                timer += Time.deltaTime;
                if (timer >= 1.5f) ChangeState((int)eState.MoveReturn);
            }
        }
    }
}