using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Actor.Player;

/// <summary>
/// プレイヤーの落下状態(ジャンプでの落下は別)を管理するステート
/// </summary>

namespace Actor.Player
{
    public class StateFall : StateBase
    {
        // ステートが始まった時に呼ばれるメソッド
        public override void OnStart(PlayerData data)
        {
            // 落下アニメーション開始

            // 下向きに速度を加える
            data.velocity = new Vector2(data.velocity.x, -0.3f);
        }

        // ステートが終了したときに呼ばれるメソッド
        public override void OnEnd(PlayerData data)
        {

        }

        // 毎フレーム呼ばれる関数
        public override void Proc(PlayerData data)
        {
            // 接地したらステート変更
            if (data.IsGround)
            {
                // 前回のステートに応じて次のステートを決める
                if (data.PrevState == eState.Run) ChangeState(eState.Run, data);
                else ChangeState(eState.Walk, data);
                return;
            }

            // 移動している方向に速度を加える
            float vx = data.velocity.x;
            float speed = (data.PrevState == eState.Run) ? 0.2f : 0.09f;
            if (Input.GetKey(KeyCode.LeftArrow)) vx = -speed;
            else if (Input.GetKey(KeyCode.RightArrow)) vx = speed;

            data.velocity = new Vector2(vx, data.velocity.y);
        }
    }
}