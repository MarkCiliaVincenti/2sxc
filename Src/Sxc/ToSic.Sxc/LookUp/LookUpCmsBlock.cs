﻿using ToSic.Eav.DataSource.Internal.Query;
using ToSic.Eav.LookUp;
using ToSic.Sxc.Blocks;
using ToSic.Sxc.Blocks.Internal;

namespace ToSic.Sxc.LookUp;

/// <inheritdoc />
/// <summary>
/// special "fake" value provider, which also transports the Sxc-dependency to underlying layers
/// </summary>
/// <inheritdoc />
/// <remarks>
/// The class constructor, can optionally take a dictionary to reference with, otherwise creates a new one
/// </remarks>
[System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
internal class LookUpCmsBlock(string name, IBlock block) : LookUpInDictionary(name, new Dictionary<string, string>
    {
        { QueryConstants.ParamsShowDraftsKey, block.Context.UserMayEdit.ToString() }
    })
{
    public IBlock Block = block;


}