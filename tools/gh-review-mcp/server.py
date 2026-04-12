"""
gh-review-mcp — MCP server for PR review operations.

Owns the approval credential (Leo1104963 PAT) and exposes exactly three
tools: pr_approve, pr_request_changes, pr_comment_review.

Only the reviewer agent should have access to this server.
"""

import os
from pathlib import Path

import httpx
from fastmcp import FastMCP

REPO = "Leo1104963/civ-game-clone"
TOKEN_PATH = Path.home() / ".claude" / "secrets" / "gh-approval-token"

mcp = FastMCP("gh-review-mcp")


def _get_token() -> str:
    token = os.environ.get("GH_APPROVAL_TOKEN")
    if token:
        return token.strip()
    if TOKEN_PATH.exists():
        return TOKEN_PATH.read_text().strip()
    raise RuntimeError(f"No approval token found in env or {TOKEN_PATH}")


def _review_request(pr_number: int, event: str, body: str) -> str:
    token = _get_token()
    url = f"https://api.github.com/repos/{REPO}/pulls/{pr_number}/reviews"
    resp = httpx.post(
        url,
        headers={
            "Authorization": f"Bearer {token}",
            "Accept": "application/vnd.github+json",
            "X-GitHub-Api-Version": "2022-11-28",
        },
        json={"event": event, "body": body},
    )
    resp.raise_for_status()
    data = resp.json()
    return f"Review submitted: {data['state']} (id={data['id']})"


@mcp.tool()
def pr_approve(pr_number: int, body: str) -> str:
    """Approve a pull request."""
    return _review_request(pr_number, "APPROVE", body)


@mcp.tool()
def pr_request_changes(pr_number: int, body: str) -> str:
    """Request changes on a pull request."""
    return _review_request(pr_number, "REQUEST_CHANGES", body)


@mcp.tool()
def pr_comment_review(pr_number: int, body: str) -> str:
    """Leave a review comment on a pull request without approving or requesting changes."""
    return _review_request(pr_number, "COMMENT", body)
