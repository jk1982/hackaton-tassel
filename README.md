
---

# 🎤 Name Audio Generator

This project is a **C# ASP.NET Core application** that analyzes a given name, detects its possible origin, transliterates it to native script (when applicable), and generates an audio file using ElevenLabs TTS.

## 🚀 Features

* ✨ Detects name ethnicity and origin (via OpenAI or Gemini).
* 🔤 Transliterates names to native scripts (e.g., Chinese, Arabic, Vietnamese) when possible.
* 🗣️ Generates spoken audio using ElevenLabs.
* 💾 Caches results in Redis to improve performance.
* 💬 Supports two AI models: **ChatGPT** (OpenAI) and **Gemini** (Google).
* ⚡ Option to use a single or dual-prompt analysis flow.

---

## 💡 How it works

1️⃣ User inputs a name on the frontend form and selects:

* Desired voice
* Model (ChatGPT or Gemini)
* Option to use a single prompt

2️⃣ Backend analyzes the name using selected AI model.

* If **single prompt**: Only initial analysis.
* If **dual prompts**: Refines native script in a second pass.

3️⃣ Backend generates an audio file of the name using ElevenLabs with chosen voice.

4️⃣ Returns JSON with analysis data and audio (Base64).

---

## ⚙️ Configuration

Set these environment variables or values in `appsettings.json` or in your deployment environment:

| Variable             | Description                                       |
| -------------------- | ------------------------------------------------- |
| `OPENAI_API_KEY`     | Your OpenAI API key.                              |
| `GEMINI_API_KEY`     | Your Google Gemini API key.                       |
| `ELEVENLABS_API_KEY` | Your ElevenLabs API key.                          |
| `REDIS_CONNECTION`   | Redis connection string (e.g., `localhost:6379`). |

---

## 🖥️ Running locally

### Prerequisites

* .NET 7 or 8 SDK
* Redis instance running locally (or remote)

### Steps

```bash
git clone https://github.com/your-repo/name-origin-audio
cd name-origin-audio
dotnet build
dotnet run
```

Then open your browser at [http://localhost:5000](http://localhost:5000) or [http://localhost:5001](http://localhost:5001).

---

## 🌐 Frontend

The frontend is a single `index.html` file that:

* Lets users enter a name.
* Lets them pick a voice.
* Choose AI model.
* Optionally use one or two prompts.
* Plays back generated audio.

---

## 📄 Example request body

```json
{
  "name": "Nguyen",
  "voiceId": "m0ym12345iHi7B3lTc2L",
  "model": "gemini",
  "singlePrompt": false
}
```

---

## ✅ Sample response

```json
{
  "native_script": "阮",
  "ethnicity": "Vietnamese",
  "confidence": 0.95,
  "alternatives": "Chinese",
  "details": "The name was converted to Vietnamese script.",
  "audio_base64": "..."
}
```

