#!/usr/bin/env python3
"""
SpaceExplorer leaderboard.
Default: 127.0.0.1:18080 (behind 1Panel reverse proxy).
"""
from flask import Flask, request, jsonify
from pathlib import Path
import json
import os
import threading

app = Flask(__name__)
BASE = Path(os.environ.get("LB_DATA_DIR", Path(__file__).parent))
DATA = BASE / "leaderboard.json"
LOCK = threading.Lock()
PORT = int(os.environ.get("LB_PORT", "18080"))
HOST = os.environ.get("LB_HOST", "127.0.0.1")


def load():
    if DATA.exists():
        try:
            return json.loads(DATA.read_text(encoding="utf-8"))
        except Exception:
            return []
    return []


def save(rows):
    DATA.parent.mkdir(parents=True, exist_ok=True)
    DATA.write_text(json.dumps(rows, ensure_ascii=False, indent=2), encoding="utf-8")


@app.route("/health")
def health():
    return jsonify({"ok": True, "service": "spaceexplorer-leaderboard"})


@app.route("/submit", methods=["POST"])
def submit():
    body = request.get_json(silent=True) or {}
    name = str(body.get("name", "匿名"))[:32]
    try:
        score = int(body.get("score", 0))
    except Exception:
        score = 0
    if score < 0:
        score = 0
    hide = bool(body.get("hide", False))
    display = body.get("display") or name
    display = str(display)[:32]

    with LOCK:
        rows = load()
        found = False
        for r in rows:
            if r.get("name") == name:
                if score > int(r.get("score", 0)):
                    r["score"] = score
                r["display"] = display
                r["hide"] = hide
                found = True
                break
        if not found:
            rows.append({"name": name, "score": score, "display": display, "hide": hide})
        rows.sort(key=lambda x: int(x.get("score", 0)), reverse=True)
        rows = rows[:50]
        save(rows)

    return jsonify({"ok": True, "name": name, "score": score})


@app.route("/top")
def top():
    with LOCK:
        rows = load()
    return jsonify(rows[:20])


@app.route("/")
def index():
    with LOCK:
        rows = load()
    lines = [
        "<!doctype html><meta charset='utf-8'><title>无尽排行</title>",
        "<h1>黄道征途 · 无尽模式排行</h1><ol>",
    ]
    for r in rows[:20]:
        disp = r.get("display") or r.get("name")
        lines.append(f"<li>{disp} — {r.get('score')}</li>")
    lines.append("</ol>")
    return "\n".join(lines)


if __name__ == "__main__":
    print(f"Leaderboard on http://{HOST}:{PORT}")
    print(f"Data file: {DATA}")
    app.run(host=HOST, port=PORT)
