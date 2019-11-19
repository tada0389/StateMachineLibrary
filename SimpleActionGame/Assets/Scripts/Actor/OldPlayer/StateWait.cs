using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Actor.Player;

/// <summary>
/// プレイヤーのアイドリング状態を管理するステート
/// </summary>

namespace Actor.Player
{
    public class StateWait : StateBase
    {
        private float timer_;

        private float tmp_vx;

        // ステートが始まった時に呼ばれるメソッド
        public override void OnStart(PlayerData data)
        {
            // 待機アニメーション開始

            tmp_vx = data.velocity.x;
            // 速度をゼロにする
            data.velocity = Vector2.zero;

            timer_ = 0.0f;
        }

        // ステートが終了したときに呼ばれるメソッド
        public override void OnEnd(PlayerData data)
        {

        }

        // 毎フレーム呼ばれる関数
        public override void Proc(PlayerData data)
        {
            timer_ += Time.deltaTime;

            // ジャンプ入力ならジャンプステートへ
            if (Input.GetKeyDown(KeyCode.Space))
            {
                ChangeState(eState.Jump, data);
                return;
            }

            // 地面から離れたらふぉるステートへ
            if (!data.IsGround)
            {
                ChangeState(eState.Fall, data);
            }

            // 左右に押したら歩くステートに変更
            if (Input.GetKey(KeyCode.RightArrow) || Input.GetKey(KeyCode.LeftArrow))
            {
                if (timer_ < 0.2f && (data.PrevState == eState.Walk || data.PrevState == eState.Run))
                {
                    // 前回移動した方向と同じか
                    if (Input.GetKey(KeyCode.RightArrow) && tmp_vx > 0f || Input.GetKey(KeyCode.LeftArrow) && tmp_vx < 0f)
                    {
                        ChangeState(eState.Run, data);
                        return;
                    }
                }

                ChangeState(eState.Walk, data);
                return;
            }
        }
    }
}