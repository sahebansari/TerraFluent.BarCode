using BenchmarkDotNet.Running;

// Run from the repository root, for example:
//   dotnet run -c Release --project benchmarks/TerraFluent.Barcode.Benchmarks -- --filter '*'
//   dotnet run -c Release --project benchmarks/TerraFluent.Barcode.Benchmarks -- --filter '*Outputs*' --job short
BenchmarkSwitcher.FromAssembly(typeof(Program).Assembly).Run(args);
