// <copyright file="ParameterProcessorMapTests.cs" company="Rnwood.SmtpServer project contributors">
// Copyright (c) Rnwood.SmtpServer project contributors. All rights reserved.
// Licensed under the BSD license. See LICENSE.md file in the project root for full license information.
// </copyright>

using System.Threading.Tasks;
using Moq;
using Xunit;

namespace Rnwood.SmtpServer.Tests;

/// <summary>
///     Defines the <see cref="ParameterProcessorMapTests" />
/// </summary>
public class ParameterProcessorMapTests
{
    /// <summary>
    ///     The GetProcessor_NotRegistered_Null
    /// </summary>
    [Fact]
    public void GetProcessor_NotRegistered_Null()
    {
        ParameterProcessorMap map = new ParameterProcessorMap();
        Assert.Null(map.GetProcessor("BLAH"));
    }

    /// <summary>
    ///     The GetProcessor_Registered_Returned
    /// </summary>
    [Fact]
    public void GetProcessor_Registered_Returned()
    {
        Mock<IParameterProcessor> processor = new Mock<IParameterProcessor>();

        ParameterProcessorMap map = new ParameterProcessorMap();
        map.SetProcessor("BLAH", processor.Object);

        Assert.Same(processor.Object, map.GetProcessor("BLAH"));
    }

    /// <summary>
    ///     The GetProcessor_RegisteredDifferentCase_Returned
    /// </summary>
    [Fact]
    public void GetProcessor_RegisteredDifferentCase_Returned()
    {
        Mock<IParameterProcessor> processor = new Mock<IParameterProcessor>();

        ParameterProcessorMap map = new ParameterProcessorMap();
        map.SetProcessor("blah", processor.Object);

        Assert.Same(processor.Object, map.GetProcessor("BLAH"));
    }

    /// <summary>
    ///     The Process_KnownParameters_Processed
    /// </summary>
    /// <returns>A <see cref="Task{T}" /> representing the async operation</returns>
    [Fact]
    public async Task Process_KnownParameters_Processed()
    {
        TestMocks mocks = new TestMocks();
        Mock<IParameterProcessor> keyAProcessor = new Mock<IParameterProcessor>();
        Mock<IParameterProcessor> keyBProcessor = new Mock<IParameterProcessor>();

        ParameterProcessorMap map = new ParameterProcessorMap();
        map.SetProcessor("keya", keyAProcessor.Object);
        map.SetProcessor("keyb", keyBProcessor.Object);

        await map.Process(mocks.Connection.Object, new[] { "KEYA=VALUEA", "KEYB=VALUEB" }, true)
            ;

        keyAProcessor.Verify(p => p.SetParameter(mocks.Connection.Object, "KEYA", "VALUEA"));
        keyBProcessor.Verify(p => p.SetParameter(mocks.Connection.Object, "KEYB", "VALUEB"));
    }

    /// <summary>
    ///     The Process_NoParameters_Accepted
    /// </summary>
    /// <returns>A <see cref="Task{T}" /> representing the async operation</returns>
    [Fact]
    public async Task Process_NoParameters_Accepted()
    {
        TestMocks mocks = new TestMocks();

        ParameterProcessorMap map = new ParameterProcessorMap();
        await map.Process(mocks.Connection.Object, new string[] { }, true);
        Assert.True(true);
    }

    /// <summary>
    ///     The Process_UnknownParameter_Throws
    /// </summary>
    /// <returns>A <see cref="Task{T}" /> representing the async operation</returns>
    [Fact]
    public async Task Process_UnknownParameter_Throws()
    {
        SmtpServerException e = await Assert.ThrowsAsync<SmtpServerException>(async () =>
        {
            TestMocks mocks = new TestMocks();

            ParameterProcessorMap map = new ParameterProcessorMap();
            await map.Process(mocks.Connection.Object, new[] { "KEYA=VALUEA" }, true);
        });

        Assert.Equal("Parameter KEYA is not recognised", e.Message);
    }
}
