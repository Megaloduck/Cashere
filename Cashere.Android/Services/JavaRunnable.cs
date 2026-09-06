using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Java.Lang;

namespace Cashere.Android.Services;

// Small adapter so a C# Action can be passed anywhere a Java Runnable is
// expected (e.g. ListenableFuture.AddListener) - standard idiom for .NET
// for Android interop with Java async APIs.
internal class JavaRunnable : Object, IRunnable
{
    private readonly Action _action;

    public JavaRunnable(Action action)
    {
        _action = action;
    }

    public void Run() => _action();
}