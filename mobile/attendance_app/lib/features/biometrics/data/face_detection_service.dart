import 'dart:math';
import 'dart:io';
import 'dart:typed_data';
import 'dart:ui';
import 'package:google_mlkit_face_detection/google_mlkit_face_detection.dart';
import 'package:image/image.dart' as img;
import '../domain/face_detection_result.dart';

class FaceDetectionService {
  FaceDetectionService()
    : _detector = FaceDetector(
        options: FaceDetectorOptions(
          performanceMode: FaceDetectorMode.accurate,
          enableClassification: true,
          enableLandmarks: false,
          minFaceSize: 0.25,
        ),
      );

  final FaceDetector _detector;

  Future<FaceDetectionResult> inspectFile(String imagePath) async {
    final input = InputImage.fromFilePath(imagePath);
    final size = await _readImageSize(imagePath);
    return _inspect(input, size);
  }

  Future<FaceDetectionResult> inspectNv21(
    Uint8List bytes,
    int width,
    int height,
    int rotation,
  ) {
    final imageRotation = InputImageRotationValue.fromRawValue(rotation);
    if (imageRotation == null) {
      throw StateError('Rotación de cámara no válida.');
    }
    final input = InputImage.fromBytes(
      bytes: bytes,
      metadata: InputImageMetadata(
        size: Size(width.toDouble(), height.toDouble()),
        rotation: imageRotation,
        format: InputImageFormat.nv21,
        bytesPerRow: width,
      ),
    );
    final sideways = rotation == 90 || rotation == 270;
    return _inspect(input, (
      width: (sideways ? height : width).toDouble(),
      height: (sideways ? width : height).toDouble(),
    ));
  }

  Future<FaceDetectionResult> _inspect(
    InputImage input,
    ({double width, double height}) imageSize,
  ) async {
    final List<Face> faces;
    try {
      faces = await _detector.processImage(input);
    } catch (error) {
      throw FaceDetectionException('ML Kit no pudo procesar la imagen', error);
    }
    if (faces.isEmpty) {
      return const FaceDetectionResult(issues: {FaceQualityIssue.noFace});
    }
    if (faces.length != 1) {
      return const FaceDetectionResult(
        issues: {FaceQualityIssue.multipleFaces},
      );
    }

    final face = faces.single;
    final issues = <FaceQualityIssue>{};
    final faceRatio = face.boundingBox.width / imageSize.width;
    if (faceRatio < 0.28) issues.add(FaceQualityIssue.tooSmall);

    final faceCenterX = face.boundingBox.center.dx / imageSize.width;
    final faceCenterY = face.boundingBox.center.dy / imageSize.height;
    if ((faceCenterX - 0.5).abs() > 0.18 || (faceCenterY - 0.48).abs() > 0.22) {
      issues.add(FaceQualityIssue.offCenter);
    }
    if ((face.headEulerAngleY ?? 0).abs() > 14) {
      issues.add(FaceQualityIssue.headTurned);
    }
    if (max(
          (face.headEulerAngleX ?? 0).abs(),
          (face.headEulerAngleZ ?? 0).abs(),
        ) >
        14) {
      issues.add(FaceQualityIssue.headTilted);
    }
    final leftEye = face.leftEyeOpenProbability;
    final rightEye = face.rightEyeOpenProbability;
    if ((leftEye != null && leftEye < 0.35) ||
        (rightEye != null && rightEye < 0.35)) {
      issues.add(FaceQualityIssue.eyesClosed);
    }
    return FaceDetectionResult(
      issues: issues,
      yaw: face.headEulerAngleY,
      leftEyeOpenProbability: leftEye,
      rightEyeOpenProbability: rightEye,
    );
  }

  Future<({double width, double height})> _readImageSize(String path) async {
    // Camera pictures are JPEGs: read the header, not every decoded pixel.
    final decoded = img.JpegDecoder().startDecode(
      await File(path).readAsBytes(),
    );
    if (decoded == null) {
      throw StateError('No se pudo decodificar la fotografía.');
    }
    return (width: decoded.width.toDouble(), height: decoded.height.toDouble());
  }

  Future<void> close() => _detector.close();
}

class FaceDetectionException implements Exception {
  const FaceDetectionException(this.stage, this.cause);
  final String stage;
  final Object cause;

  @override
  String toString() => '$stage: $cause';
}
