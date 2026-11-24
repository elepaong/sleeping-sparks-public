// 필드의 사물 하나 (그림 없는 규칙 쪽). 모양·위치·반응은 모두 data/zones/<id>.json의 항목(def)에서 온다.
using System.Collections.Generic;

namespace Sparks.Core
{
    using Dict = Dictionary<string, object>;

    public class FieldObject
    {
        public Dict def;
        public string id, type, state, initialState;
        public Cell cell = new Cell(-1, -1);   // 칸 위의 사물이면 칸 좌표
        public bool hasCell;
        public bool beamTransient;             // 지금 상태가 '빛이 닿아 있는 동안만'인 상태인가
        public V2 pos;                         // 맵 픽셀 (y는 아래가 +)
        public bool visible = true;

        readonly GameData data;
        readonly State st;

        public FieldObject(Dict d, Dict grid, string savedState, GameData data, State st)
        {
            this.data = data; this.st = st;
            def = d;
            id = J.Str(d, "id");
            type = J.Str(d, "type", "prop");
            initialState = J.Str(d, "state", "default");
            state = savedState ?? initialState;
            if (J.Has(d, "cell") && grid != null && grid.Count > 0)
            {
                hasCell = true;
                var c = J.List(d, "cell");
                cell = new Cell((int)J.Num(c[0]), (int)J.Num(c[1]));
                var o = J.List(grid, "origin"); var s = J.List(grid, "cell");
                pos = new V2(J.At(o, 0) + (cell.x + 0.5f) * J.At(s, 0), J.At(o, 1) + (cell.y + 0.5f) * J.At(s, 1));
            }
            else
            {
                var p = J.List(d, "pos");
                pos = new V2(J.At(p, 0), J.At(p, 1));
            }
        }

        public bool Flat { get { return J.Bool(def, "flat"); } }
        /// <summary>그림이 가로로 프레임을 이은 시트면 그 수 (지역 데이터 "frames", 없으면 1). 모든 상태의 그림이 같은 수.</summary>
        public int Frames { get { return System.Math.Max(1, (int)J.Num(def, "frames", 1)); } }
        /// <summary>애니메이션 빠르기 (초당 프레임, "fps", 없으면 6)</summary>
        public float Fps { get { return (float)J.Num(def, "fps", 6); } }
        public bool HasLabel { get { return Config.ShowLabels && J.Bool(def, "label", true) && J.Has(def, "name_key"); } }

        public bool SetState(string s)
        {
            if (s == state) return false;
            state = s;
            return true;
        }

        /// <summary>지금 상태의 그림 경로 ("assets/…"). 그림이 없으면 "".</summary>
        public string SpritePath
        {
            get
            {
                if (J.Has(def, "sprites")) return J.Str(J.Get(def, "sprites"), state);
                return J.Str(def, "sprite");
            }
        }

        /// <summary>그림의 왼쪽 위 모서리와 크기 (맵 좌표).</summary>
        public RectF SpriteRect()
        {
            int[] size = data.ImageSize(SpritePath);
            size = new[] { size[0] / Frames, size[1] };   // 시트면 한 칸 크기
            float ox = 0, oy = 0;
            if (J.Has(def, "sprite_offset")) { var o = J.List(def, "sprite_offset"); ox = J.At(o, 0); oy = J.At(o, 1); }
            if (Flat) return new RectF(pos.x - size[0] / 2f + ox, pos.y - size[1] / 2f + oy, size[0], size[1]);
            return new RectF(pos.x - size[0] / 2f + ox, pos.y - size[1] + 8 + oy, size[0], size[1]);   // 발치가 사물 위치보다 조금 아래
        }

        /// <summary>지금 화면에 있는가 (visible_if 조건, 숨는 상태)</summary>
        public void UpdateVisibility()
        {
            bool v = true;
            if (J.Has(def, "visible_if") && !st.Check(J.Get(def, "visible_if"))) v = false;
            if (J.Contains(J.List(def, "hidden_states"), state)) v = false;
            if (type == "ink" && (state == "taken" || !st.investigate)) v = false;
            visible = v;
        }

        /// <summary>클릭·이름표 영역 (맵 좌표)</summary>
        public RectF Hotspot()
        {
            if (J.Has(def, "hotspot"))
            {
                var h = J.List(def, "hotspot");
                return new RectF(J.At(h, 0), J.At(h, 1), J.At(h, 2), J.At(h, 3));
            }
            return SpriteRect();
        }

        /// <summary>걸음을 막는 영역 (없으면 크기 0)</summary>
        public RectF SolidRect()
        {
            if (!visible || !J.Has(def, "solid")) return new RectF();
            var s = J.List(def, "solid");
            return new RectF(pos.x - J.At(s, 0) / 2f, pos.y - J.At(s, 1) / 2f, J.At(s, 0), J.At(s, 1));
        }

        /// <summary>스파크를 놓았을 때 스파크가 설 자리. 벽 선반처럼 그림이 위치보다 높이 그려진 사물은 "place_offset"만큼 옮긴다.</summary>
        public V2 PlacePos
        {
            get
            {
                if (!J.Has(def, "place_offset")) return pos;
                var o = J.List(def, "place_offset");
                return pos + new V2(J.At(o, 0), J.At(o, 1));
            }
        }

        public bool Placeable { get { return visible && J.Bool(def, "placeable") && st.Check(J.Get(def, "place_if")); } }

        public bool CanInteract
        {
            get
            {
                if (!visible) return false;
                if (type == "ink") return true;
                return type == "mirror" || type == "lens_slot" || J.Has(def, "interact");
            }
        }

        public BeamInfo BeamInfo() { return new BeamInfo { id = id, type = type, state = state, blocksBeam = J.Bool(def, "blocks_beam") }; }
    }
}
