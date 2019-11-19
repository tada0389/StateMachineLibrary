﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Actor.Player;

/// <summary>
/// プレイヤーの歩き状態を管理するステート
/// 接地している時のみ
/// </summary>

namespace Actor.Player
{
    public class StateWalk : StateBase
    {

        private float init_speed_ = 0.09f;

        // ステートが始まった時に呼ばれるメソッド
        public override void OnStart(PlayerData data)
        {
            // 歩きアニメーション開始
            //data.animator.Play("Walk");

            // 移動している方向に速度を加える
            float vx = 0.0f;
            if (Input.GetKey(KeyCode.LeftArrow)) vx = -init_speed_;
            else if (Input.GetKey(KeyCode.RightArrow)) vx = init_speed_;

            data.velocity = new Vector2(vx, 0f);
        }

        // ステートが終了したときに呼ばれるメソッド
        public override void OnEnd(PlayerData data)
        {

        }

        // 毎フレーム呼ばれる関数
        public override void Proc(PlayerData data)
        {
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

            // 移動している方向に速度を加える
            float vx = 0.0f;
            if (Input.GetKey(KeyCode.LeftArrow)) vx = -init_speed_;
            else if (Input.GetKey(KeyCode.RightArrow)) vx = init_speed_;
            else
            {
                // 何も押していないならWait状態に
                ChangeState(eState.Wait, data);
                return;
            }

            data.velocity = new Vector2(vx, data.velocity.y);
        }
    }
}