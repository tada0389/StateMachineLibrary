using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Actor.NewPlayer;
using TadaLib;

/// <summary>
/// プレイヤーのアイドリング状態を管理するステート
/// </summary>

namespace Actor.NewPlayer
{
    public partial class Player
    {
        [System.Serializable]
        private class StateIdle : StateMachine<Player>.StateBase
        {
            // ステート間で共有するデータのコピーインスタンス
            private Data data = null;

            private float timer_;

            private float saved_vx;

            // ステートが始まった時に呼ばれるメソッド
            public override void OnStart()
            {
                if(data == null) data = Parent.data_;

                // 待機アニメーション開始
                data.animator.Play("Idle");

                saved_vx = data.velocity.x;
                // 速度をゼロにする
                data.velocity = Vector2.zero;

                timer_ = 0.0f;
            }

            // ステートが終了したときに呼ばれるメソッド
            public override void OnEnd()
            {

            }

            // 毎フレーム呼ばれる関数
            public override void Proc()
            {
                timer_ += Time.deltaTime;

                // ジャンプ入力ならジャンプステートへ
                if (Input.GetKeyDown(KeyCode.Space))
                {
                    ChangeState((int)eState.Jump);
                    return;
                }

                // 地面から離れたらふぉるステートへ
                if (!data.IsGround)
                {
                    ChangeState((int)eState.Fall);
                }

                // 左右に押したら歩くステートに変更
                if (Input.GetKey(KeyCode.RightArrow) || Input.GetKey(KeyCode.LeftArrow))
                {
                    if (timer_ < 0.2f && (PrevStateId == (int)eState.Walk || PrevStateId == (int)eState.Run))
                    {
                        // 前回移動した方向と同じか
                        if (Input.GetKey(KeyCode.RightArrow) && saved_vx > 0f || Input.GetKey(KeyCode.LeftArrow) && saved_vx < 0f)
                        {
                            ChangeState((int)eState.Run);
                            return;
                        }
                    }

                    ChangeState((int)eState.Walk);
                    return;
                }
            }
        }
    }
}