using System;

namespace TextComposing.IO.Exchange
{
    public interface IExchangableText
    {
        void Accept(IExchangableTextVisitor visitor);
    }

    public interface IExchangableTextVisitor
    {
        void Letter(UChar letter);

        void RubyStart(UString rubyText);
        void RubyEnd();

        void EmphasysDotStart();
        void EmphasysDotEnd();
    }
}
