using System;

namespace TextComposing.LineBreaking
{
    internal class ConstantFrame : IFrameModel
    {
        private float _length;

        public ConstantFrame(float length)
        {
            _length = length;
        }

        float LineBreaking.IFrameModel.LengthOf(int lineNumber)
        {
            return _length;
        }
    }

    /// <summary>
    /// IFrameModel のファクトリ
    /// </summary>
    public static class Frame
    {
        public static IFrameModel Constant(float length)
        {
            return new ConstantFrame(length);
        }
    }
}
