using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Actor.Player;

namespace Actor.Player
{
    public class StateBase
    {
        // ステートが始まった時に呼ばれるメソッド
        public virtual void OnStart(PlayerData data)
        {

        }

        // ステートが終了したときに呼ばれるメソッド
        public virtual void OnEnd(PlayerData data)
        {

        }

        // 毎フレーム呼ばれる関数
        public virtual void Proc(PlayerData data)
        {

        }

        // ステートを変更する 第一引数に変更後のステート，第二引数には・・・
        protected void ChangeState(eState new_state, PlayerData data)
        {
            // この変更だと，1Fの遅延が生じてしまう(Procが次のフレームに呼ばれる)? OnStartで初期化するから大丈夫？
            data.state_queue.Enqueue(new_state);
        }
    }
}