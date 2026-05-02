using System;

namespace SC.Contract;

public static class Assembly
{
    public static System.Reflection.Assembly Get => typeof(Assembly).Assembly;
}
