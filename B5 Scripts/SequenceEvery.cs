using System.Collections.Generic;
using System.Diagnostics;
using System;
using UnityEngine;

namespace TreeSharpPlus
{
    /// <summary>
    ///   The base sequence class. This will execute each branch of logic, in order.
    ///   If all branches succeed, this composite will return a successful run status.
    ///   If any branch fails, this composite will return a failed run status.
    /// </summary>
    public class SequenceEvery : NodeGroup
    {
        protected Stopwatch stopwatch;
        protected long waitMax;

        public SequenceEvery(Val<long> waitMax, params Node[] children)
            : base(children)
        {
            this.waitMax = waitMax.Value;
            this.stopwatch = new Stopwatch();
        }

        public override void Start()
        {
            base.Start();
        }

        public override void Stop()
        {
            base.Stop();
            this.stopwatch.Stop();
        }

        public override IEnumerable<RunStatus> Execute()
        {
            if(!this.stopwatch.IsRunning)
                this.stopwatch.Reset();
                this.stopwatch.Start();
            while(true){
                if (this.stopwatch.ElapsedMilliseconds >= this.waitMax)
                {
                    this.stopwatch.Stop();
                    foreach (Node node in this.Children)
                    {
                        // Move to the next node
                        this.Selection = node;
                        node.Start();

                        // If the current node is still running, report that. Don't 'break' the enumerator
                        RunStatus result;
                        while ((result = this.TickNode(node)) == RunStatus.Running)
                            yield return RunStatus.Running;

                        // Call Stop to allow the node to clean anything up.
                        node.Stop();

                        // Clear the selection
                        this.Selection.ClearLastStatus();
                        this.Selection = null;

                        if (result == RunStatus.Failure)
                        {
                            yield return RunStatus.Failure;
                            yield break;
                        }

                        yield return RunStatus.Running;
                    }
                    yield return RunStatus.Success;
                    yield break;
                }
                yield return RunStatus.Running;
            }
        }
    }
}