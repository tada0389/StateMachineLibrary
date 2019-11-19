using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Actor.Player;
using TadaLib;

/// <summary>
/// プレイヤーのジャンプ状態を管理するステート
/// </summary>

namespace Actor.Player
{
    public class StateJump : StateBase
    {
        // ジャンプしてからの経過時間
        private float timer_;

        // ステートが始まった時に呼ばれるメソッド
        public override void OnStart(PlayerData data)
        {
            // ジャンプアニメーション開始

            timer_ = 0.0f;

            // 上向きに速度を加える
            data.velocity = new Vector2(data.velocity.x, 0.35f);
        }

        // ステートが終了したときに呼ばれるメソッド
        public override void OnEnd(PlayerData data)
        {

        }

        // 毎フレーム呼ばれる関数
        public override void Proc(PlayerData data)
        {
            timer_ += Time.deltaTime;

            // 移動している方向に速度を加える
            float vx = 0f;
            float speed = (data.PrevState == eState.Run) ? 0.2f : 0.09f;
            if (Input.GetKey(KeyCode.LeftArrow)) vx = -speed;
            else if (Input.GetKey(KeyCode.RightArrow)) vx = speed;

            data.velocity = new Vector2(vx, (timer_ < 0.3f) ? 0.35f : -0.35f);

            // 接地したらステート変更 ジャンプはじめはIsGroundがtrueになってたので一定時間が経ったら
            if (data.IsGround && timer_ > 0.2f)
            {
                // 前回のステートに応じて次のステートを決める
                if (data.PrevState == eState.Run) ChangeState(eState.Run, data);
                else ChangeState(eState.Walk, data);
                return;
            }
        }
    }
}