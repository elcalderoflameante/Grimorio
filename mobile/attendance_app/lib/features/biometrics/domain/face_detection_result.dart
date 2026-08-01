enum FaceQualityIssue {
  noFace,
  multipleFaces,
  tooSmall,
  offCenter,
  headTurned,
  headTilted,
  eyesClosed,
}

class FaceDetectionResult {
  const FaceDetectionResult({
    required this.issues,
    this.yaw,
    this.leftEyeOpenProbability,
    this.rightEyeOpenProbability,
  });
  final Set<FaceQualityIssue> issues;
  final double? yaw;
  final double? leftEyeOpenProbability;
  final double? rightEyeOpenProbability;
  bool get isAcceptable => issues.isEmpty;
  bool get hasSingleFace =>
      !issues.contains(FaceQualityIssue.noFace) &&
      !issues.contains(FaceQualityIssue.multipleFaces);
}
