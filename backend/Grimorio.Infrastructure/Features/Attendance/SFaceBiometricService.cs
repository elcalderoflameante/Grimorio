using OpenCvSharp;
using OpenCvSharp.Dnn;

namespace Grimorio.Infrastructure.Features.Attendance;

public sealed record FaceEmbeddingResult(
    float[] Embedding,
    double DetectionConfidence,
    double FaceWidthRatio,
    double FaceHeightRatio,
    double HorizontalCenterOffset,
    double VerticalCenterOffset);

/// <summary>
/// Extracts normalized SFace embeddings. YuNet is used only to detect the face
/// and provide the five landmarks required by SFace alignment.
/// </summary>
public sealed class SFaceBiometricService : IDisposable
{
    public const string ModelVersion = "opencv-sface-2021dec-v2";
    public const int EmbeddingDimensions = 128;
    public const int MaximumImageBytes = 5 * 1024 * 1024;

    private readonly SemaphoreSlim _gate = new(1, 1);
    private readonly string _faceDetectorPath;
    private readonly string _faceRecognizerPath;
    private Net? _recognizer;

    private static readonly Point2f[] SFaceReferenceLandmarks =
    [
        new(38.2946f, 51.6963f),
        new(73.5318f, 51.5014f),
        new(56.0252f, 71.7366f),
        new(41.5493f, 92.3655f),
        new(70.7299f, 92.2041f)
    ];

    public SFaceBiometricService()
    {
        var modelDirectory = Path.Combine(AppContext.BaseDirectory, "Biometrics");
        _faceDetectorPath = Path.Combine(modelDirectory, "face_detection_yunet_2023mar.onnx");
        _faceRecognizerPath = Path.Combine(modelDirectory, "face_recognition_sface_2021dec.onnx");
    }

    public bool ModelsAvailable => File.Exists(_faceDetectorPath) && File.Exists(_faceRecognizerPath);

    public void ValidateModels() => EnsureModelsLoaded();

    public async Task<FaceEmbeddingResult> ExtractEmbeddingAsync(byte[] encodedImage,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(encodedImage);
        if (encodedImage.Length == 0 || encodedImage.Length > MaximumImageBytes)
            throw new ArgumentException("La imagen facial debe tener entre 1 byte y 5 MB.", nameof(encodedImage));

        await _gate.WaitAsync(cancellationToken);
        try
        {
            EnsureModelsLoaded();
            using var image = Cv2.ImDecode(encodedImage, ImreadModes.Color);
            if (image.Empty()) throw new ArgumentException("La imagen facial no tiene un formato válido.", nameof(encodedImage));

            using var detector = FaceDetectorYN.Create(_faceDetectorPath, string.Empty, image.Size(), 0.9f, 0.3f, 5000);
            using var faces = new Mat();
            detector.Detect(image, faces);
            if (faces.Rows == 0) throw new InvalidOperationException("No se detectó un rostro.");
            if (faces.Rows > 1) throw new InvalidOperationException("Debe aparecer una sola persona.");

            using var face = faces.Row(0);
            using var aligned = AlignFace(image, face);
            // Match OpenCV FaceRecognizerSF::feature exactly. SFace expects raw
            // 0..255 pixels with BGR-to-RGB conversion, without mean subtraction.
            using var blob = CvDnn.BlobFromImage(aligned, 1.0, new Size(112, 112),
                Scalar.All(0), swapRB: true, crop: false);
            _recognizer!.SetInput(blob);
            using var features = _recognizer.Forward();

            var embedding = new float[checked((int)features.Total())];
            features.GetArray(out embedding);
            Normalize(embedding);
            var faceWidth = face.Get<float>(0, 2);
            var faceHeight = face.Get<float>(0, 3);
            var faceCenterX = face.Get<float>(0, 0) + faceWidth / 2.0;
            var faceCenterY = face.Get<float>(0, 1) + faceHeight / 2.0;
            return new FaceEmbeddingResult(
                embedding,
                face.Get<float>(0, 14),
                faceWidth / image.Width,
                faceHeight / image.Height,
                Math.Abs(faceCenterX / image.Width - 0.5),
                Math.Abs(faceCenterY / image.Height - 0.5));
        }
        finally
        {
            _gate.Release();
        }
    }

    public static double CosineSimilarity(ReadOnlySpan<float> first, ReadOnlySpan<float> second)
    {
        if (first.Length == 0 || first.Length != second.Length)
            throw new ArgumentException("Los vectores biométricos deben tener la misma dimensión.");

        double dot = 0;
        double normFirst = 0;
        double normSecond = 0;
        for (var index = 0; index < first.Length; index++)
        {
            dot += first[index] * second[index];
            normFirst += first[index] * first[index];
            normSecond += second[index] * second[index];
        }

        return dot / (Math.Sqrt(normFirst) * Math.Sqrt(normSecond));
    }

    private void EnsureModelsLoaded()
    {
        if (_recognizer is not null) return;
        if (!ModelsAvailable)
            throw new InvalidOperationException("Los modelos biométricos SFace/YuNet no están instalados.");

        _recognizer = CvDnn.ReadNetFromOnnx(_faceRecognizerPath);
    }

    private static Mat AlignFace(Mat image, Mat face)
    {
        var detectedLandmarks = new Point2f[5];
        for (var index = 0; index < detectedLandmarks.Length; index++)
            detectedLandmarks[index] = new Point2f(face.Get<float>(0, 4 + index * 2), face.Get<float>(0, 5 + index * 2));

        using var source = Mat.FromArray(detectedLandmarks);
        using var destination = Mat.FromArray(SFaceReferenceLandmarks);
        using var transform = Cv2.EstimateAffinePartial2D(source, destination);
        if (transform is null || transform.Empty())
            throw new InvalidOperationException("No se pudo alinear el rostro detectado.");

        var aligned = new Mat();
        Cv2.WarpAffine(image, aligned, transform, new Size(112, 112), InterpolationFlags.Linear,
            BorderTypes.Constant, Scalar.All(0));
        return aligned;
    }

    private static void Normalize(Span<float> values)
    {
        double squaredLength = 0;
        foreach (var value in values) squaredLength += value * value;
        var length = Math.Sqrt(squaredLength);
        if (length <= double.Epsilon) throw new InvalidOperationException("SFace generó un vector biométrico vacío.");
        for (var index = 0; index < values.Length; index++) values[index] = (float)(values[index] / length);
    }

    public void Dispose()
    {
        _recognizer?.Dispose();
        _gate.Dispose();
    }
}
