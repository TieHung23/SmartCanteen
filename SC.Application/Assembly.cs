using System;

namespace SC.Application;

public static class Assembly
{
    public static System.Reflection.Assembly Get => typeof(Assembly).Assembly;
}
