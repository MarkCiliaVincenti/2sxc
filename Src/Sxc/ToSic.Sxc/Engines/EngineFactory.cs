﻿using ToSic.Lib.DI;
using ToSic.Lib.Services;
using ToSic.Sxc.Blocks.Internal;

namespace ToSic.Sxc.Engines;

[System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
public class EngineFactory: ServiceBase
{

    public EngineFactory(Generator<IRazorEngine> razorEngineGen, Generator<TokenEngine> tokenEngineGen): base($"{SxcLogName}.EngFct")
    {
        ConnectServices(
            _razorEngineGen = razorEngineGen,
            _tokenEngineGen = tokenEngineGen
        );
    }
    private readonly Generator<IRazorEngine> _razorEngineGen;
    private readonly Generator<TokenEngine> _tokenEngineGen;

    public IEngine CreateEngine(IView view) => view.IsRazor
        ? _razorEngineGen.New()
        : _tokenEngineGen.New();
}