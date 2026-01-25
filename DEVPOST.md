# DREAMAIL

**Reimagining Email in Virtual Reality**

DREAMAIL transforms the traditional email experience into an immersive 3D environment where emails become interactive spheres you can grab, toss, and organize naturally in VR space.

---

## What it does

DREAMAIL is a revolutionary VR email client for Meta Quest that brings your inbox to life in three dimensions. Instead of scrolling through endless lists, emails appear as colorful spheres floating around you in virtual space. 

**Core Features:**

- **Real-time Email Visualization**: Emails from your Gmail inbox materialize as 3D spheres in VR, updating in real-time via Google Cloud Pub/Sub
- **AI-Powered Summaries**: Each email is automatically analyzed and summarized using Google Gemini AI, so you can instantly understand what matters
- **Voice-First Interaction**: Use speech-to-text to dictate quick email responses without typing
- **AI Email Generation**: Speak a brief message like "schedule meeting next week" and DREAMAIL generates a complete professional email with proper formatting and signature
- **Intelligent Categorization**: Emails are automatically classified (Work, Personal, Promotions, Social) and prioritized (1-5) using AI
- **Natural VR Interactions**: Grab spheres to read emails, throw them toward your view to snap them into focus, or toss them away to dismiss
- **Audio Summaries**: Text-to-speech narration of email summaries for hands-free review
- **Sender Avatars**: Gravatar integration displays profile pictures for instant sender recognition

---

## How we built it

**Platform & Framework:**
- Built in **Unity** for Meta Quest VR headsets
- **Oculus Integration SDK** for hand tracking and VR interactions
- **C#** for all game logic and API integrations

**Cloud & AI Services:**
- **Google Cloud Pub/Sub**: Real-time email notification pipeline
- **Gmail API**: Email fetching and management
- **Google Gemini AI**: Email summarization and categorization
- **Google Speech-to-Text API**: Voice recognition for hands-free commands
- **ElevenLabs API**: Neural text-to-speech for audio summaries
- **Gravatar API**: Sender profile image fetching

**Key Technical Systems:**

1. **Email Sphere System** (`EmailSphere.cs`): Physics-based 3D objects with grab mechanics, velocity-based snapping, and focus states
2. **Speech Recognition** (`SpeechRecognition.cs`): Records microphone input, converts to WAV format, and sends to Google Speech API
3. **AI Email Generation** (`GenerateEmail.cs`): Transforms brief voice input into full professional emails with proper structure
4. **Email Analysis** (`EmailAITool.cs`): Multi-faceted AI analysis including summarization, categorization, and priority scoring
5. **Real-time Sync** (`GCloudPubSubManager.cs`): Maintains live connection to Gmail via Pub/Sub for instant notifications
6. **State Management**: Global state system for managing focused spheres and UI states

**Architecture:**
- Singleton pattern for core services (Audio, SpeechRecognition, EmailAITool)
- Event-driven communication using UnityEvents
- Coroutine-based async API calls
- Component-based design for modular email sphere features

---

## Challenges we ran into

**1. Audio Format Compatibility Crisis**
- Initially received MP3 audio from ElevenLabs API but Unity's WAV parser only supported PCM format
- Error: "Format code '19459' detected, but only PCM formats supported"
- **Solution**: Switched to Unity's native `DownloadHandlerAudioClip` with `AudioType.MPEG` for automatic decoding

**2. Real-time Speech Recognition Limitations**
- Google's batch Speech API (`speech:recognize`) processes complete audio files, not streaming
- Unity's `UnityWebRequest` doesn't support bidirectional streaming (WebSocket/gRPC)
- **Solution**: Implemented "record then transcribe" workflow with clear visual feedback states

**3. Method Overload Ambiguity**
- `EmailSphere.Initialize()` had multiple overloads with nullable parameters
- Compiler couldn't determine if `null` meant `AudioClip` or `Texture2D`
- **Solution**: Explicit casting `(AudioClip)null` to disambiguate the call

**4. VR Physics & Interaction Tuning**
- Balancing sphere throw velocity, snap thresholds, and focus mechanics
- Too sensitive = accidental snapping; too strict = frustrating interactions
- **Solution**: Iterative testing with configurable thresholds (velocity < 0.2m/s, angle < 45°)

**5. API Cost Management**
- ElevenLabs audio generation was expensive for every email
- **Solution**: Made audio generation optional, prioritized text summaries, added capability to cache audio clips

**6. Async API Chain Complexity**
- Email arrives → Analyze (Gemini) → Generate Audio (ElevenLabs) → Spawn Sphere
- Each step async with potential failures
- **Solution**: Robust coroutine chains with fallbacks (e.g., use snippet if summary fails)

---

## Accomplishments that we're proud of

✨ **Seamless VR-AI Integration**: Successfully chained multiple AI APIs (Gemini, ElevenLabs, Google Speech) into a cohesive VR experience

🎯 **Natural Interaction Design**: The sphere physics feel intuitive - grab, throw, snap behaviors emerged from careful tuning

🚀 **Real-time Pipeline**: Achieved true real-time email notifications in VR using Google Cloud Pub/Sub

🎤 **Voice-First Innovation**: Built a complete voice-to-email pipeline where you can dictate a few words and get a professional email

🧠 **Intelligent Email Understanding**: AI doesn't just summarize - it categorizes, prioritizes, and generates contextually appropriate responses

💎 **Production-Quality Architecture**: Singleton services, event-driven design, and proper error handling make this a maintainable codebase

📧 **Complete Email Experience**: From notification to reading to responding, all in immersive 3D space

---

## What we learned

**Technical Insights:**
- Unity's audio system has specific format requirements - always use native handlers when possible
- VR interaction design requires extensive playtesting and tunable parameters
- Async API chains need robust error handling and graceful degradation
- Coroutines in Unity are powerful but require careful lifecycle management

**AI Integration:**
- Google Gemini is remarkably effective at understanding email context and generating summaries
- Prompt engineering matters - specific instructions like "1-2 sentences max" produce better results
- Always provide fallbacks for AI services (use original text if summarization fails)

**VR Design Principles:**
- Physical metaphors work best in VR (throwing to dismiss, grabbing to read)
- Visual state feedback is crucial (Selecting → Detecting → Generating → Displaying)
- Minimize text entry in VR - voice input is far more natural

**Cloud Architecture:**
- Pub/Sub provides reliable real-time notifications without polling
- API key authentication is simpler than OAuth for prototypes
- Always monitor API costs in production scenarios

---

## What's next for DREAMAIL

**Near-term Enhancements:**
- **True Streaming Speech**: Integrate WebSocket for real-time transcription as you speak
- **Gesture Commands**: Hand gestures for quick actions (pinch to delete, wave to archive)
- **Spatial Organization**: Sort emails into 3D zones (Urgent section, Read Later wall, Archive pile)
- **Multi-language Support**: Extend voice recognition and email generation to multiple languages
- **Calendar Integration**: Visualize meetings and events as different 3D objects

**Advanced Features:**
- **AI Email Coaching**: Real-time suggestions for improving drafted emails
- **Sentiment Analysis**: Visual indicators for email tone (urgent/angry vs. casual/friendly)
- **Thread Visualization**: Email chains as connected sphere clusters
- **Collaborative Spaces**: Shared VR email workspaces for teams
- **Smart Notifications**: Learn which emails are actually important to you

**Platform Expansion:**
- **Apple Vision Pro** support with eye-tracking navigation
- **Passthrough Mode** for augmented reality email overlay
- **Desktop Companion App** for traditional email when not in VR
- **Integration with Slack, Teams** for unified communication hub

**Research Directions:**
- **VR Productivity Studies**: Measure if 3D email management actually improves efficiency
- **Accessibility Features**: Voice-only navigation for visually impaired users
- **Privacy & Security**: End-to-end encryption for VR email communications

---

## Built With

`unity` `csharp` `meta-quest` `oculus` `google-cloud` `gemini-ai` `google-speech-api` `elevenlabs` `gmail-api` `pub-sub` `vr` `virtual-reality` `ai` `machine-learning` `text-to-speech` `speech-to-text` `natural-language-processing`

---

**Try DREAMAIL and experience email like never before - in immersive 3D space! 🚀**
