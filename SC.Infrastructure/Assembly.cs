using System;

namespace SC.Infrastructure;

public static class Assembly
{
    public static System.Reflection.Assembly Get => typeof(Assembly).Assembly;
}
