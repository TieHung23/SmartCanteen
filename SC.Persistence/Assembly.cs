using System;
using System.Reflection;

namespace SC.Persistence;

public static class Assembly
{
    public static System.Reflection.Assembly Get => typeof(Assembly).Assembly;
}
