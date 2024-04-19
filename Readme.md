# K8s Windows Node Monitor

This tool has been built to monitor Windows nodes running in AKS, and make the various issues visible to us.

## Installation

helm install node-monitor charts/node-monitor

## Architecture

The node monitor consists of a non-privileged and privileged daemonset running on Windows nodes. These two containers communicate with one another to identify and report issues with the node.

* Privileged pod is responsible for detecting crash dumps, watching the node event log, and notifying the non-privileged pod of failures.
* Non-privileged pod is responsible for detecting internal container DNS resolution failures, and reporting all failures from both pods via email.

Code was written to automatically reboot nodes in the event of a failure threshold being breached, but this has never been enabled - all these pods have ever done is report issues via email.

## Current issues

The primary issue this was built to identify is as follows:

* Pod loses network connectivity due to HNS crash (open with Microsoft under case ID 2312040050004324). Two patches have been provided by Microsoft so far, neither have been successful.

We're also monitoring the following issues in the cluster currently (both open with Microsoft under case ID 2404160050003263):

* Containers won’t start, error “The system has attempted to load or restore a file into the registry, but the specified file is not in a registry file format.: unknown in that environment”
* Node can’t pull images from ACR

There is some discussion that this node monitor itself may be partially responsible for the volume of the second two issues we've been seeing recently, although the registry issue has been seen prior to this tool's creation. This tool has been removed from the cluster so we can identify whether it's a causal factor.