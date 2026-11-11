import 'dart:io';

import 'package:flutter/services.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:image/image.dart' as img;
import 'package:grimorio_attendance/features/biometrics/data/face_detection_service.dart';
import 'package:grimorio_attendance/features/biometrics/domain/face_detection_result.dart';

void main() {
  TestWidgetsFlutterBinding.ensureInitialized();
  const channel = MethodChannel('google_mlkit_face_detector');
  final messenger =
      TestDefaultBinaryMessengerBinding.instance.defaultBinaryMessenger;
  late List<Map<String, dynamic>> faces;
  late FaceDetectionService service;

  Map<String, dynamic> face({
    double left = 150,
    double top = 190,
    double right = 330,
    double bottom = 430,
    double yaw = 0,
    double tilt = 0,
    double eye = 0.9,
  }) => {
    'rect': {'left': left, 'top': top, 'right': right, 'bottom': bottom},
    'headEulerAngleX': 0.0,
    'headEulerAngleY': yaw,
    'headEulerAngleZ': tilt,
    'leftEyeOpenProbability': eye,
    'rightEyeOpenProbability': eye,
    'landmarks': <String, dynamic>{},
  };

  setUp(() {
    faces = [face()];
    messenger.setMockMethodCallHandler(channel, (call) async {
      if (call.method == 'vision#startFaceDetector') return faces;
      return null;
    });
    service = FaceDetectionService();
  });
  tearDown(() async {
    await service.close();
    messenger.setMockMethodCallHandler(channel, null);
  });

  Future<FaceDetectionResult> inspect() =>
      service.inspectNv21(Uint8List(1), 640, 480, 90);

  test('acepta un rostro frontal centrado en la imagen NV21 rotada', () async {
    expect((await inspect()).isAcceptable, isTrue);
  });

  test(
    'rechaza ausencia, varias personas, distancia, giro e inclinación',
    () async {
      final cases = <(List<Map<String, dynamic>>, FaceQualityIssue)>[
        ([], FaceQualityIssue.noFace),
        ([face(), face()], FaceQualityIssue.multipleFaces),
        ([face(left: 220, right: 260)], FaceQualityIssue.tooSmall),
        ([face(left: 0, right: 180)], FaceQualityIssue.offCenter),
        ([face(yaw: 20)], FaceQualityIssue.headTurned),
        ([face(tilt: 20)], FaceQualityIssue.headTilted),
        ([face(eye: 0.1)], FaceQualityIssue.eyesClosed),
      ];
      for (final (detected, issue) in cases) {
        faces = detected;
        expect((await inspect()).issues, contains(issue));
      }
    },
  );

  test('normaliza dimensiones para las cuatro rotaciones de cámara', () async {
    for (final rotation in [0, 90, 180, 270]) {
      final width = rotation == 90 || rotation == 270 ? 480.0 : 640.0;
      final height = rotation == 90 || rotation == 270 ? 640.0 : 480.0;
      faces = [
        face(
          left: width * .3,
          right: width * .7,
          top: height * .25,
          bottom: height * .7,
        ),
      ];
      final result = await service.inspectNv21(
        Uint8List(1),
        640,
        480,
        rotation,
      );
      expect(result.isAcceptable, isTrue, reason: 'Rotación $rotation');
    }
  });

  test(
    'JPEG con orientación EXIF usa coordenadas verticales de ML Kit',
    () async {
      faces = [face(left: 170, right: 310)];
      final directory = await Directory.systemTemp.createTemp(
        'attendance-exif-',
      );
      addTearDown(() => directory.delete(recursive: true));
      final original = img.Image(width: 640, height: 480);
      final jpeg = img.encodeJpg(original);
      for (final orientation in [5, 6, 7, 8]) {
        final exif = img.ExifData()..imageIfd.orientation = orientation;
        final rotated = img.injectJpgExif(jpeg, exif)!;
        final file = File('${directory.path}/orientation-$orientation.jpg');
        await file.writeAsBytes(rotated);
        expect(
          (await service.inspectFile(file.path)).isAcceptable,
          isTrue,
          reason: 'EXIF $orientation',
        );
      }
    },
  );

  test('un error de ML Kit no se interpreta como rostro válido', () async {
    messenger.setMockMethodCallHandler(channel, (_) async {
      throw PlatformException(code: 'detector_failed');
    });
    await expectLater(inspect(), throwsA(isA<FaceDetectionException>()));
    messenger.setMockMethodCallHandler(channel, (_) async => null);
  });
}
