# AstroLogger [![License](https://img.shields.io/badge/license-Apache%202.0-blue)](https://github.com/Contykpo/AstroLogger/blob/master/LICENSE)

## Overview

AstroLogger is an early-stage .NET logging library initially oriented towards .NET applications. It is easy to set up, has a clean API, and runs on all recent .NET platforms. While it's useful even in the simplest applications, AstroLogger's support for structured logging shines when instrumenting complex, distributed, and asynchronous applications and systems.

Like many other libraries for .NET, AstroLogger provides diagnostic logging to files, the console, and many other outputs.

***

## The Plan

The plan for AstroLogger is two-fold: to create a powerful logging library built from the ground up to record structured event data, but also to open up a huge range of diagnostic possibilities not available when using traditional loggers.

### Main features to come:

- Format-based logging API with familiar levels of severity such as Debug, Information, Warning, Error, and so-on.
- Discoverable C# configuration syntax and optional JSON configuration support.
- Efficient when enabled, extremely low overhead when a logging level is switched off.
- Best-in-class .NET Core support, including rich integration with ASP.NET Core. 
- Support for a comprehensive range of sinks, including files, the console, on-premises and cloud-based log servers, databases, and message queues.
- Sophisticated enrichment of log events provided with a custom Log Format with contextual information, including scoped properties, thread and process identifiers.
- Zero-shared-state Logger objects, with an optional global static Logger class (currently default).
- Format-agnostic logging pipeline that can emit events in plain text, JSON, and in-memory logs.
