// Desc: psd转ui
// Author: ollve
// Date: 2020-06-19

namespace Editor.PSD2UI
{
    /// <summary>
    /// psd脚本导出的xml结构
    /// </summary>
    public class PSDUI
    {
        public Size psdSize;
        public Layer[] layers;

        public enum LayerType
        {
            Normal,
            Button,
            Label,
            Text,
            Image,
        }

        public class Layer
        {
            public string name;
            public LayerType type;
            public Arguments arguments;
            public Position position;
            public Size size;
            public string parent;
        }

        public class Arguments
        {
            public string color;
            public string size;
        }

        public class Position
        {
            public float x;
            public float y;
        }

        public class Size
        {
            public float width;
            public float height;
        }

    }
}