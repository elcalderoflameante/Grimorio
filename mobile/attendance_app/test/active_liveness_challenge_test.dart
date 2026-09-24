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
  test('reinicia al perder el rostro después de cerrar los ojos', () {
    final challenge = ActiveLivenessChallenge();
    challenge.process(frame());
    challenge.process(frame());
    challenge.process(frame(left: .1, right: .1));
    challenge.process(
      const FaceDetectionResult(issues: {FaceQualityIssue.noFace}),
    );
    challenge.process(frame());
    expect(challenge.isCompleted, isFalse);
    expect(challenge.step, LivenessStep.center);
  });

  test('caduca un parpadeo incompleto', () {
    final challenge = ActiveLivenessChallenge();
    final now = DateTime.utc(2026);
    challenge.process(frame(), now: now);
    challenge.process(frame(), now: now);
    challenge.process(frame(left: .1, right: .1), now: now);
    challenge.process(frame(), now: now.add(const Duration(seconds: 9)));
    expect(challenge.step, LivenessStep.center);
  });

  test('reinicia si cambia la calidad o aparecen varias personas', () {
    for (final issue in [
      FaceQualityIssue.offCenter,
      FaceQualityIssue.multipleFaces,
    ]) {
      final challenge = ActiveLivenessChallenge();
      challenge.process(frame());
      challenge.process(frame());
      challenge.process(FaceDetectionResult(issues: {issue}));
      expect(challenge.step, LivenessStep.center);
    }
  });
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
