#!/usr/bin/php
<?php

  /**
   *   GENERATES UPDATE SCRIPT FOR MINECRAFT RESOURCES
   *   
   *   WORKS WITH 1.8+
   **/
 
  if (!@$argv[1]) die("Usage: {$argv[0]} <version> [directory prefix]\n");
  
  function printl($t) { fwrite(STDERR, $t); }
  
  @$dir_prefix = $argv[2];
  if (!$dir_prefix) $dir_prefix = "";
  else if (substr($dir_prefix, -1) != "/") $dir_prefix = $dir_prefix."/";
  
  
  printl("Downloading version json ({$argv[1]})... ");
  $version = $argv[1];
  if ($version != "legacy") 
  {
    $resource = json_decode(file_get_contents("http://s3.amazonaws.com/Minecraft.Download/versions/$version/$version.json"), true);
    $assets = $resource["assets"];
  }
  else $assets = "legacy";
  if (!$assets) die("Unable to extract assets\n");
  printl("Assets version: $assets\n");
  
  printl("Download assets version... ");
  $assets_version = $assets;
  $assets_data = file_get_contents("https://s3.amazonaws.com/Minecraft.Download/indexes/$assets.json");
  $assetsj = json_decode($assets_data, true);
  $assets = $assetsj["objects"];
  printl("Found ".count($assets)." assets\n");
  
  if ($dir_prefix) echo "CANFAIL MKDIR \"$dir_prefix\"\n";
  echo "CANFAIL MKDIR \"{$dir_prefix}indexes\"\n";
  echo "OVERRIDE \"{$dir_prefix}indexes/$assets_version.json\" \"https://s3.amazonaws.com/Minecraft.Download/indexes/$assets_version.json\"\n";
  echo "ADD \"{$dir_prefix}indexes/$assets_version.json\" \"".sha1($assets_data)."\"\n";
  echo "CANFAIL MKDIR \"{$dir_prefix}objects\"\n";
  $created_dirs = array();
  foreach ($assets as $file => $asset)
  {
    $asset = $asset["hash"];
    $dir = substr($asset, 0, 2);
    if (!isset($created_dirs[$dir]))
    {
      echo "CANFAIL MKDIR \"{$dir_prefix}objects/$dir\"\n";
      $created_dirs[$dir] = true;
    }
    $output = "{$dir_prefix}objects/$dir/$asset";
    echo "OVERRIDE \"$output\" \"http://resources.download.minecraft.net/$dir/$asset\"\n";
    echo "ADD \"$output\" \"$asset\"\n";
  }
