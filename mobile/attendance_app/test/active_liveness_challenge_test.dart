import 'package:flutter_test/flutter_test.dart';
import 'package:grimorio_attendance/features/biometrics/domain/active_liveness_challenge.dart';
import 'package:grimorio_attendance/features/biometrics/domain/face_detection_result.dart';

FaceDetectionResult frame({double left = .9, double right = .9}) =>
    FaceDetectionResult(
      issues: const {},
      leftEyeOpenProbability: left,
      rightEyeOpenProbability: right,
    );

void main() {
  test('completa dos cuadros centrados y un parpadeo', () {
    final challenge = ActiveLivenessChallenge();
    challenge.process(frame());
    challenge.process(frame());
    expect(challenge.step, LivenessStep.blink);
    challenge.process(frame(left: .1, right: .1));
    challenge.process(frame());
    expect(challenge.isCompleted, isTrue);
  });

  test('no avanza sin un unico rostro', () {
    final challenge = ActiveLivenessChallenge();
    challenge.process(
      const FaceDetectionResult(issues: {FaceQualityIssue.noFace}),
    );
    expect(challenge.step, LivenessStep.center);
  });
}
