import 'face_detection_result.dart';

enum LivenessStep { center, blink, completed }

class ActiveLivenessChallenge {
  LivenessStep _step = LivenessStep.center;
  int _centerFrames = 0;
  bool _eyesWereOpen = false;
  bool _eyesWereClosed = false;

  LivenessStep get step => _step;
  bool get isCompleted => _step == LivenessStep.completed;

  String get instruction => switch (_step) {
    LivenessStep.center => 'Mira de frente',
    LivenessStep.blink => 'Parpadea lentamente una vez',
    LivenessStep.completed => 'Prueba de vida completada',
  };

  void process(FaceDetectionResult result) {
    if (!result.hasSingleFace || isCompleted) return;

    switch (_step) {
      case LivenessStep.center:
        if (result.isAcceptable) {
          _centerFrames++;
          if (_centerFrames >= 2) {
            _eyesWereOpen = true;
            _step = LivenessStep.blink;
          }
        } else {
          _centerFrames = 0;
        }
        return;
      case LivenessStep.blink:
        final left = result.leftEyeOpenProbability;
        final right = result.rightEyeOpenProbability;
        if (left == null || right == null) return;
        if (left > 0.65 && right > 0.65) _eyesWereOpen = true;
        if (_eyesWereOpen && left < 0.3 && right < 0.3) {
          _eyesWereClosed = true;
        }
        if (_eyesWereClosed && left > 0.65 && right > 0.65) {
          _step = LivenessStep.completed;
        }
        return;
      case LivenessStep.completed:
        return;
    }
  }
}
