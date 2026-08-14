"""
Scaffolds a CQRS vertical slice: the six mechanical files.

docs/01ARCHITECTURE.md §3 fixes the shape of a slice at seven files - Command
xor Query, then Request, Response, Validator, Mapper, Endpoint. Six of those
are the same shape every time and only the Handler carries real logic, so six
are generated and the Handler is written by hand.

The point is not typing speed. It is that a slice cannot quietly come out a
different shape depending on who or which tool wrote it, which matters with
several developers on several different assistants. The architecture tests
check the result; this stops the mistake being made.

Usage:
    python build/slices.py                 # regenerate every slice in SPECS
    python build/slices.py SignIn          # one slice by name

A file is NEVER overwritten if it already exists, except the ones marked
regenerable below. Hand edits win: the generator is a starting point, not an
owner.
"""
import sys
from pathlib import Path

ROOT = Path(__file__).resolve().parent.parent
MODULES = ROOT / "src" / "Backend" / "Modules"

# Files the generator may rewrite. Everything else is written once and then
# belongs to whoever edits it.
REGENERABLE = set()


def xml_escape(text):
    """
    Makes prose safe inside an XML doc comment.

    "Roles & Capabilities" is a correct screen name and invalid XML, and CS1570
    is an error under this build's settings.
    """
    return text.replace("&", "&amp;").replace("<", "&lt;").replace(">", "&gt;")


def field_list(fields, indent="    "):
    return ",\n".join(f"{indent}{t} {n}" for t, n in fields)


def command_source(spec):
    kind = "ICommand" if spec["kind"] == "command" else "IQuery"
    return f"""using AMS.SharedKernel.Messaging;

namespace {spec['ns']}.Features.{spec['name']};

/// <summary>
/// {xml_escape(spec['summary'])}
/// </summary>
public sealed record {spec['name']}{spec['suffix']}(
{field_list(spec['command'])}) : {kind}<{spec['name']}Response>;
"""


def request_source(spec):
    if not spec["request"]:
        return f"""namespace {spec['ns']}.Features.{spec['name']};

/// <summary>
/// The HTTP wire shape. Empty: this slice takes everything it needs from the
/// route and the caller's identity (docs/01 §3).
/// </summary>
public sealed record {spec['name']}Request;
"""
    return f"""namespace {spec['ns']}.Features.{spec['name']};

/// <summary>
/// The HTTP wire shape. Never a domain entity in either direction (docs/01 §3).
/// </summary>
public sealed record {spec['name']}Request(
{field_list(spec['request'])});
"""


def response_source(spec):
    params = "\n".join(
        f"/// <param name=\"{n}\">{xml_escape(spec['responseDocs'].get(n, 'See the handler.'))}</param>"
        for _, n in spec["response"]
    )
    return f"""namespace {spec['ns']}.Features.{spec['name']};

/// <summary>
/// {xml_escape(spec['responseSummary'])}
/// </summary>
{params}
public sealed record {spec['name']}Response(
{field_list(spec['response'])});
"""


def validator_source(spec):
    rules = "\n".join(f"        {r}" for r in spec["rules"]) or \
        "        // Nothing to check: the slice takes no caller-supplied fields."
    return f"""using FluentValidation;

namespace {spec['ns']}.Features.{spec['name']};

/// <summary>
/// Shape only. Lengths mirror the schema exactly.
/// </summary>
/// <remarks>
/// Business invariants are NOT here. "Already taken", "already allocated" and
/// "one active per X" are filtered unique indexes, and a read-then-write check
/// is a race with a nicer error message (docs/02 §5, 03 §1 rule 6).
///
/// Every Request has a validator, even a trivial one, so nobody forgets when a
/// field is added later.
/// </remarks>
public sealed class {spec['name']}Validator : AbstractValidator<{spec['name']}Request>
{{
    public {spec['name']}Validator()
    {{
{rules}
    }}
}}
"""


def mapper_source(spec):
    args = ",\n            ".join(spec["mapArgs"])
    to = "ToCommand" if spec["kind"] == "command" else "ToQuery"
    extra = "".join(f", {t} {n}" for t, n in spec.get("mapExtra", []))
    return f"""namespace {spec['ns']}.Features.{spec['name']};

/// <summary>
/// Request to {'command' if spec['kind'] == 'command' else 'query'}. Explicit,
/// greppable, compile-checked - no AutoMapper (docs/02 §4).
/// </summary>
public static class {spec['name']}Mapper
{{
    public static {spec['name']}{spec['suffix']} {to}({spec['name']}Request request{extra})
    {{
        ArgumentNullException.ThrowIfNull(request);

        return new {spec['name']}{spec['suffix']}(
            {args});
    }}
}}
"""


def endpoint_source(spec):
    verb = spec["verb"]
    binder = spec.get("bind", "")
    to = "ToCommand" if spec["kind"] == "command" else "ToQuery"
    result = spec.get("result", "ToHttpResult()")
    produces = spec.get("successStatus", "Status200OK")
    extra_produces = "\n".join(
        f"            .Produces(StatusCodes.{s})" for s in spec.get("otherStatuses", [])
    )

    # Three kinds of gate, and the difference matters:
    #   anonymous  - sign-in itself; there is nobody to authorise yet
    #   authenticated only - "my profile": every signed-in user may read their
    #     own record, and a capability would be a lie because withdrawing it
    #     would lock somebody out of their own password change
    #   capability - everything else (01 §2 rule 6)
    if spec.get("anonymous"):
        gate = "            .AllowAnonymous()"
    elif spec.get("capability"):
        gate = f"            .RequireCapability({spec['capability']})"
    else:
        gate = "            .RequireAuthorization()"
    # Only when the endpoint actually binds it: an unused using is IDE0005,
    # which this build treats as an error.
    abstractions = "using AMS.SharedKernel.Abstractions;\n" if "ICurrentUser" in binder else ""

    return f"""{abstractions}using AMS.SharedKernel.Messaging;
using AMS.SharedKernel.Web.Http;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace {spec['ns']}.Features.{spec['name']};

/// <summary>
/// Route, capability, typed results. Zero logic - if you feel an <c>if</c>
/// coming on, it belongs in the handler (docs/02 §6).
/// </summary>
public static class {spec['name']}Endpoint
{{
    public static void Map(RouteGroupBuilder group)
    {{
        ArgumentNullException.ThrowIfNull(group);

        group.Map{verb}("{spec['route']}", async (
{binder}                IDispatcher dispatcher,
                CancellationToken ct) =>
            {{
                var message = {spec['name']}Mapper.{to}({spec['mapCall']});
                var result = await dispatcher.SendAsync(message, ct);

                return result.{result};
            }})
{gate}
            .WithName("{spec['name']}")
            .Produces<{spec['name']}Response>(StatusCodes.{produces})
            .ProducesValidationProblem()
{extra_produces}
            ;
    }}
}}
"""


WRITERS = {
    "{name}{suffix}.cs": command_source,
    "{name}Request.cs": request_source,
    "{name}Response.cs": response_source,
    "{name}Validator.cs": validator_source,
    "{name}Mapper.cs": mapper_source,
    "{name}Endpoint.cs": endpoint_source,
}


def generate(spec):
    spec.setdefault("suffix", "Command" if spec["kind"] == "command" else "Query")
    spec.setdefault("responseDocs", {})
    folder = MODULES / spec["project"] / "Features" / spec["name"]
    folder.mkdir(parents=True, exist_ok=True)

    written, skipped = [], []
    for template, writer in WRITERS.items():
        filename = template.format(name=spec["name"], suffix=spec["suffix"])
        path = folder / filename
        if path.exists() and filename not in REGENERABLE:
            skipped.append(filename)
            continue
        path.write_text(writer(spec), encoding="utf-8", newline="\n")
        written.append(filename)

    handler = folder / f"{spec['name']}Handler.cs"
    status = "HANDLER EXISTS" if handler.exists() else "HANDLER TO WRITE"
    print(f"  {spec['name']:28} {len(written)} written, {len(skipped)} kept   [{status}]")
    return handler.exists()


def main(specs):
    wanted = sys.argv[1:] if len(sys.argv) > 1 else None
    missing = []
    for spec in specs:
        if wanted and spec["name"] not in wanted:
            continue
        if not generate(spec):
            missing.append(spec["name"])
    if missing:
        print("\nHandlers still to write (the only file with real logic):")
        for name in missing:
            print(f"  - {name}Handler.cs")
